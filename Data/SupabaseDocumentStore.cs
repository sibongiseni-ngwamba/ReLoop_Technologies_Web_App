using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

namespace ReLoop_Technologies_Web_App.Data;

/// <summary>Maps the legacy collection interface onto the normalized tables shared with the Android app.</summary>
public sealed class SupabaseDocumentStore(string connectionString) : IReLoopDocumentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, TableMap> Tables = new Dictionary<string, TableMap>(StringComparer.Ordinal)
    {
        ["users"] = new("profiles", "id", true), ["agents"] = new("agents", "agent_id"),
        ["pickups"] = new("pickups", "pickup_id"), ["wasteLogs"] = new("waste_logs", "waste_log_id"),
        ["wasteCategories"] = new("waste_categories", "waste_category_id"), ["guidanceContent"] = new("guidance_content", "guidance_id"),
        ["rewards"] = new("reward_accounts", "user_id"), ["rewardItems"] = new("reward_items", "reward_item_id"),
        ["redemptionLogs"] = new("redemption_logs", "redemption_id"), ["pointsTransactions"] = new("points_transactions", "transaction_id"),
        ["notifications"] = new("notifications", "notification_id"), ["contactMessages"] = new("contact_messages", "contact_id")
    };

    public async Task<T?> GetAsync<T>(string collection, string documentId) where T : class
    {
        var table = Resolve(collection);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"select to_jsonb(row_data) from public.{table.Name} as row_data where {table.PrimaryKey}::text = @id", connection);
        command.Parameters.AddWithValue("id", documentId);
        var value = await command.ExecuteScalarAsync();
        return value is string json ? Deserialize<T>(json, table.IsProfile) : null;
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(string collection) where T : class
    {
        var table = Resolve(collection);
        var results = new List<T>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"select to_jsonb(row_data) from public.{table.Name} as row_data order by {table.PrimaryKey}", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            if (Deserialize<T>(reader.GetString(0), table.IsProfile) is { } item) results.Add(item);
        return results;
    }

    public async Task SaveAsync<T>(string collection, string documentId, T document) where T : class
    {
        var table = Resolve(collection);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await SaveAsync(connection, null, table, documentId, document);
    }

    public async Task DeleteAsync(string collection, string documentId)
    {
        var table = Resolve(collection);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await DeleteAsync(connection, null, table, documentId);
    }

    public async Task<TResult> RunTransactionAsync<TResult>(Func<IReLoopDocumentTransaction, Task<TResult>> operation)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var documentTransaction = new SupabaseDocumentTransaction(connection, transaction);
        var result = await operation(documentTransaction);
        await documentTransaction.FlushAsync();
        await transaction.CommitAsync();
        return result;
    }

    private static TableMap Resolve(string collection) => Tables.TryGetValue(collection, out var map)
        ? map : throw new InvalidOperationException($"Unknown ReLoop collection '{collection}'.");

    private static async Task SaveAsync<T>(NpgsqlConnection connection, NpgsqlTransaction? transaction, TableMap table, string id, T document) where T : class
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(document, JsonOptions))!.AsObject();
        foreach (var item in json.ToArray())
        {
            json.Remove(item.Key);
            json[ToSnake(item.Key)] = item.Value?.DeepClone();
        }
        if (table.IsProfile)
        {
            if (json.Remove("user_id", out var userId)) json["id"] = userId;
            if (json["role"] is JsonValue role && role.TryGetValue<string>(out var roleName))
                json["role"] = roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "admin" : roleName.Equals("Collector", StringComparison.OrdinalIgnoreCase) ? "collector" : "resident";
        }
        await using var command = new NpgsqlCommand("select public.upsert_reloop_row(@table, @primary_key, @document_id, @payload::jsonb)", connection, transaction);
        command.Parameters.AddWithValue("table", table.Name);
        command.Parameters.AddWithValue("primary_key", table.PrimaryKey);
        command.Parameters.AddWithValue("document_id", id);
        command.Parameters.AddWithValue("payload", json.ToJsonString());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, TableMap table, string id)
    {
        await using var command = new NpgsqlCommand($"delete from public.{table.Name} where {table.PrimaryKey}::text = @id", connection, transaction);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }

    private static T? Deserialize<T>(string json, bool isProfile) where T : class
    {
        var node = JsonNode.Parse(json)?.AsObject();
        if (node is null) return null;
        foreach (var item in node.ToArray())
        {
            node.Remove(item.Key);
            node[ToCamel(item.Key)] = item.Value?.DeepClone();
        }
        if (isProfile)
        {
            if (node.Remove("id", out var id)) node["userId"] = id;
            if (node["role"] is JsonValue role && role.TryGetValue<string>(out var roleName))
                node["role"] = roleName.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : roleName.Equals("collector", StringComparison.OrdinalIgnoreCase) ? "Collector" : "Member";
            node["preferredCategory"] ??= "Recyclables";
        }
        return node.Deserialize<T>(JsonOptions);
    }

    private static string ToSnake(string value) => string.Concat(value.Select((character, index) => char.IsUpper(character) ? (index > 0 ? "_" : "") + char.ToLowerInvariant(character) : character.ToString()));
    private static string ToCamel(string value) => System.Text.RegularExpressions.Regex.Replace(value, "_([a-z])", match => match.Groups[1].Value.ToUpperInvariant());
    private sealed record TableMap(string Name, string PrimaryKey, bool IsProfile = false);

    private sealed class SupabaseDocumentTransaction(NpgsqlConnection connection, NpgsqlTransaction transaction) : IReLoopDocumentTransaction
    {
        private readonly List<Func<Task>> pending = [];

        public async Task<T?> GetAsync<T>(string collection, string documentId) where T : class
        {
            var table = Resolve(collection);
            await using var command = new NpgsqlCommand($"select to_jsonb(row_data) from public.{table.Name} as row_data where {table.PrimaryKey}::text = @id for update", connection, transaction);
            command.Parameters.AddWithValue("id", documentId);
            var value = await command.ExecuteScalarAsync();
            return value is string json ? Deserialize<T>(json, table.IsProfile) : null;
        }

        public void Set<T>(string collection, string documentId, T document) where T : class
        {
            var table = Resolve(collection);
            pending.Add(() => SaveAsync(connection, transaction, table, documentId, document));
        }

        public void Delete(string collection, string documentId)
        {
            var table = Resolve(collection);
            pending.Add(() => DeleteAsync(connection, transaction, table, documentId));
        }

        public async Task FlushAsync()
        {
            foreach (var write in pending) await write();
        }
    }
}
