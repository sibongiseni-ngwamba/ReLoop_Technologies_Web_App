namespace ReLoop_Technologies_Web_App.Data.Entities;

public sealed class UserAccount
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public string Address { get; set; } = "452 Eco Circular Ave, Suite 3B";
    public string PreferredCategory { get; set; } = "Recyclables";
    public int RewardPoints { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Pickup> Pickups { get; set; } = [];
    public List<ScanRecord> ScanRecords { get; set; } = [];
    public List<ActivityLog> ActivityLogs { get; set; } = [];
    public List<RewardLedgerEntry> RewardLedger { get; set; } = [];
}
