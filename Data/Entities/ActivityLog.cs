namespace ReLoop_Technologies_Web_App.Data.Entities;

public sealed class ActivityLog
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Tone { get; set; } = "neutral";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserAccount? UserAccount { get; set; }
}
