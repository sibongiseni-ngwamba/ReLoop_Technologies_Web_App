namespace ReLoop_Technologies_Web_App.Data.Entities;

public sealed class RewardRedemption
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public Guid RewardCatalogueItemId { get; set; }
    public int PointsSpent { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public UserAccount? UserAccount { get; set; }
    public RewardCatalogueItem? RewardCatalogueItem { get; set; }
}
