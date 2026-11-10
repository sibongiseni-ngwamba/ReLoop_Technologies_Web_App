using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReLoop_Technologies_Web_App.Services;

public sealed record SupabaseIdentity(string Uid, string Email, string DisplayName);

public interface ISupabaseIdentityService
{
    Task<SupabaseIdentity?> VerifyAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}

/// <summary>Validates access tokens with Supabase Auth's user endpoint.</summary>
public sealed class SupabaseIdentityService(HttpClient httpClient, IConfiguration configuration) : ISupabaseIdentityService
{
    public async Task<SupabaseIdentity?> VerifyAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return null;
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/v1/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("apikey", configuration["Supabase:AnonKey"]);
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            var user = await response.Content.ReadFromJsonAsync<SupabaseUser>(cancellationToken: cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(user.Email)) return null;
            var name = MetadataValue(user.UserMetadata, "full_name")
                ?? MetadataValue(user.UserMetadata, "name")
                ?? user.Email;
            return new SupabaseIdentity(user.Id, user.Email, name);
        }
        catch (HttpRequestException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }
    }

    private static string? MetadataValue(Dictionary<string, System.Text.Json.JsonElement>? metadata, string key) =>
        metadata is not null && metadata.TryGetValue(key, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed record SupabaseUser(string Id, string? Email, Dictionary<string, System.Text.Json.JsonElement>? UserMetadata);
}
