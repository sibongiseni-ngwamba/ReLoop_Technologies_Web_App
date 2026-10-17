using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace ReLoop_Technologies_Web_App.Services;

public sealed record FirebaseIdentity(string Uid, string Email, string DisplayName);

public interface IFirebaseIdentityService
{
    Task<FirebaseIdentity?> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}

public sealed class FirebaseIdentityService : IFirebaseIdentityService
{
    private readonly FirebaseAuth auth;

    public FirebaseIdentityService(string projectId)
    {
        var appName = $"reloop-{projectId}";
        var app = FirebaseApp.Create(new AppOptions
        {
            ProjectId = projectId,
            Credential = GoogleCredential.GetApplicationDefault()
        }, appName);
        auth = FirebaseAuth.GetAuth(app);
    }

    public async Task<FirebaseIdentity?> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await auth.VerifyIdTokenAsync(idToken, checkRevoked: true, cancellationToken: cancellationToken);
            var email = token.Claims.TryGetValue("email", out var rawEmail) ? rawEmail?.ToString() : null;
            var name = token.Claims.TryGetValue("name", out var rawName) ? rawName?.ToString() : null;
            if (string.IsNullOrWhiteSpace(email)) return null;
            return new FirebaseIdentity(token.Uid, email, name ?? email);
        }
        catch (FirebaseAuthException)
        {
            return null;
        }
    }
}
