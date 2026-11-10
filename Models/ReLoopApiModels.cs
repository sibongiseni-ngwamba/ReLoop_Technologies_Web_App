using System.ComponentModel.DataAnnotations;

namespace ReLoop_Technologies_Web_App.Models;

public sealed record AuthRequest(
    [property: Required] string IdToken,
    bool KeepSignedIn = false);

public sealed record SignUpRequest(
    [property: Required] string IdToken,
    [property: Required, StringLength(120, MinimumLength = 2)] string FullName);

public sealed record ProfileUpdateRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string FullName,
    [property: Required, EmailAddress] string Email,
    [property: Required, StringLength(240, MinimumLength = 8)] string Address,
    [property: Required, StringLength(80)] string PreferredCategory);

public sealed record ContactRequest(
    [property: Required] string FullName,
    [property: Required, EmailAddress] string Email,
    [property: Required, StringLength(140)] string Subject,
    [property: Required, StringLength(1200, MinimumLength = 10)] string Message);

public sealed record CreatePickupRequest(
    [property: Required, StringLength(240, MinimumLength = 8)] string HomeAddress,
    [property: Required] DateTime PreferredDate,
    [property: Required, StringLength(60)] string PreferredTimeWindow,
    [property: Required, StringLength(80)] string WasteCategory,
    [property: StringLength(500)] string? Notes);

public sealed record MetricDto(string Label, string Value, string Detail, string Tone);
public sealed record ActivityDto(string Title, string Detail, string Date, string Tone);
public sealed record PickupDto(string Id, string Date, string Address, string WasteType, string Weight, string Status);
public sealed record ScanResultDto(string Item, string Disposition, string Category, string EstimatedWeight, int Points, int Confidence, DateTimeOffset DateScanned);
public sealed record WasteSliceDto(string Type, int Percent, string Tone);
public sealed record DashboardDto(string UserName, IEnumerable<MetricDto> Metrics, IEnumerable<ActivityDto> Activity, IEnumerable<PickupDto> Pickups);
public sealed record AdminStatsDto(IEnumerable<MetricDto> Metrics, IEnumerable<WasteSliceDto> WasteDistribution, IEnumerable<PickupDto> PendingPickups);
public sealed record ProfileDto(string FullName, string Email, string Role, string Address, string PreferredCategory, int RewardPoints, int ScanCount, int PickupCount);
public sealed record AdminUserDto(string Id, string FullName, string Email, string Role, bool IsActive, int RewardPoints, int Pickups, string Joined, string Address, string PreferredCategory);
public sealed record AdminPickupDto(string Id, string Reference, string Member, string Date, string Material, string Status, string? AssignedAgent, string Weight);
public sealed record AdminActivityDto(string Member, string Title, string Detail, string Date, string Tone);
public sealed record AdminRewardDto(string Id, string Name, string Description, int PointsCost, bool IsActive);
public sealed record AdminRedemptionDto(string Id, string Member, string Reward, int PointsSpent, string Status, string Date);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record AdminOverviewDto(IEnumerable<MetricDto> Metrics, IEnumerable<WasteSliceDto> WasteDistribution);
public sealed record AdminUserUpdateRequest([property: Required, StringLength(120)] string FullName, [property: Required, RegularExpression("^(Member|Admin)$")] string Role, bool IsActive, [property: Required, StringLength(240)] string Address, [property: Required, StringLength(80)] string PreferredCategory);
public sealed record AdminPickupUpdateRequest([property: Required, RegularExpression("^(Scheduled|Assigned|Completed|Cancelled)$")] string Status, [property: StringLength(120)] string? AssignedAgent);
public sealed record RewardCatalogueRequest([property: Required, StringLength(120)] string Name, [property: Required, StringLength(400)] string Description, [property: Range(1, 100000)] int PointsCost, bool IsActive);
public sealed record RedemptionUpdateRequest([property: Required, RegularExpression("^(Pending|Approved|Fulfilled|Declined)$")] string Status);
public sealed record RedeemRewardRequest([property: Required, StringLength(140)] string RewardId);
public sealed record RewardOptionDto(string Id, string Name, string Description, int PointsCost);
public sealed record MemberRedemptionDto(string Id, string Reward, int PointsSpent, string Status, DateTimeOffset CreatedAt);
public sealed record RewardsDto(int Points, IReadOnlyList<RewardOptionDto> Catalogue, IReadOnlyList<MemberRedemptionDto> Redemptions);
public sealed record WasteGuidanceDto(string Category, string Preparation, string AcceptedItems, string ExcludedItems);
