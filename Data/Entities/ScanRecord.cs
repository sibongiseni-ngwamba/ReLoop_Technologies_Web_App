namespace ReLoop_Technologies_Web_App.Data.Entities;

public sealed class ScanRecord
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Disposition { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal EstimatedWeightKg { get; set; }
    public int PointsAwarded { get; set; }
    public int Confidence { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset ScannedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserAccount? UserAccount { get; set; }
}
