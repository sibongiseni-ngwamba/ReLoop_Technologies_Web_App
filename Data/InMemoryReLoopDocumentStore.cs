using System.Text.Json;

namespace ReLoop_Technologies_Web_App.Data;

public sealed class InMemoryReLoopDocumentStore : IReLoopDocumentStore
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private Dictionary<string, object> documents = new(StringComparer.Ordinal);

    public async Task<T?> GetAsync<T>(string collection, string documentId) where T : class
    {
        await gate.WaitAsync();
        try { return Read<T>(documents, collection, documentId); }
        finally { gate.Release(); }
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(string collection) where T : class
    {
        await gate.WaitAsync();
        try
        {
            var prefix = $"{collection}/";
            return documents.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal)).Select(pair => Clone<T>(pair.Value)).ToArray();
        }
        finally { gate.Release(); }
    }

    public async Task SaveAsync<T>(string collection, string documentId, T document) where T : class
    {
        await gate.WaitAsync();
        try { documents[Key(collection, documentId)] = Clone<T>(document); }
        finally { gate.Release(); }
    }

    public async Task DeleteAsync(string collection, string documentId)
    {
        await gate.WaitAsync();
        try { documents.Remove(Key(collection, documentId)); }
        finally { gate.Release(); }
    }

    public async Task<TResult> RunTransactionAsync<TResult>(Func<IReLoopDocumentTransaction, Task<TResult>> operation)
    {
        await gate.WaitAsync();
        try
        {
            var working = documents.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            var result = await operation(new MemoryTransaction(working));
            documents = working;
            return result;
        }
        finally { gate.Release(); }
    }

    private static string Key(string collection, string id) => $"{collection}/{id}";
    private static T? Read<T>(Dictionary<string, object> source, string collection, string id) where T : class =>
        source.TryGetValue(Key(collection, id), out var document) ? Clone<T>(document) : null;

    private static T Clone<T>(object value) where T : class =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value)) ?? throw new InvalidOperationException("Could not copy in-memory document.");

    private sealed class MemoryTransaction(Dictionary<string, object> documents) : IReLoopDocumentTransaction
    {
        public Task<T?> GetAsync<T>(string collection, string documentId) where T : class => Task.FromResult(Read<T>(documents, collection, documentId));
        public void Set<T>(string collection, string documentId, T document) where T : class => documents[Key(collection, documentId)] = Clone<T>(document);
        public void Delete(string collection, string documentId) => documents.Remove(Key(collection, documentId));
    }
}
