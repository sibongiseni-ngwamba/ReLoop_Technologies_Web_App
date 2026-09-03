namespace ReLoop_Technologies_Web_App.Data.Entities;

public sealed class RewardLedgerEntry
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserAccount? UserAccount { get; set; }
}
