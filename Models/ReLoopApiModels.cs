using System.ComponentModel.DataAnnotations;

namespace ReLoop_Technologies_Web_App.Models;

public sealed record AuthRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(6)] string Password,
    bool KeepSignedIn = false);

public sealed record SignUpRequest(
    [property: Required] string FullName,
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(6)] string Password,
    [property: Required, Compare("Password")] string ConfirmPassword);

public sealed record ProfileUpdateRequest(
    [property: Required] string FullName,
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(8)] string Address,
    [property: Required] string PreferredCategory);

public sealed record ContactRequest(
    [property: Required] string FullName,
    [property: Required, EmailAddress] string Email,
    [property: Required] string Subject,
    [property: Required, MinLength(10)] string Message);

public sealed record CreatePickupRequest(
    [property: Required, MinLength(8)] string HomeAddress,
    [property: Required] DateTime PreferredDate,
    [property: Required] string PreferredTimeWindow,
    [property: Required] string WasteCategory,
    string? Notes);

public sealed record MetricDto(string Label, string Value, string Detail, string Tone);
public sealed record ActivityDto(string Title, string Detail, string Date, string Tone);
public sealed record PickupDto(string Id, string Date, string Address, string WasteType, string Weight, string Status);
public sealed record ScanResultDto(string Item, string Disposition, string Category, string EstimatedWeight, int Points, int Confidence, DateTimeOffset DateScanned);
public sealed record WasteSliceDto(string Type, int Percent, string Tone);
public sealed record DashboardDto(string UserName, IEnumerable<MetricDto> Metrics, IEnumerable<ActivityDto> Activity, IEnumerable<PickupDto> Pickups);
public sealed record AdminStatsDto(IEnumerable<MetricDto> Metrics, IEnumerable<WasteSliceDto> WasteDistribution, IEnumerable<PickupDto> PendingPickups);
public sealed record ProfileDto(string FullName, string Email, string Role, string Address, string PreferredCategory, int RewardPoints, int ScanCount, int PickupCount);
