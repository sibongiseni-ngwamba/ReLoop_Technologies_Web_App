using ReLoop_Technologies_Web_App.Models;

namespace ReLoop_Technologies_Web_App.Services;

public sealed class ReLoopStore
{
    private readonly List<PickupDto> _pickups =
    [
        new("#LP-9082", "12 May 2025, 09:00 AM", "452 Eco Circular Ave, Suite 3B", "Recyclables", "Pending", "Scheduled"),
        new("#LP-8931", "08 May 2025, 02:30 PM", "452 Eco Circular Ave, Suite 3B", "Organic", "4.5 kg", "Completed"),
        new("#LP-8742", "05 May 2025, 11:20 AM", "452 Eco Circular Ave, Suite 3B", "Recyclables", "1.2 kg", "Completed"),
        new("#LP-8521", "28 Apr 2025, 10:00 AM", "710 Greenwood Terrace", "E-waste", "--", "Cancelled")
    ];

    private readonly List<ActivityDto> _activity =
    [
        new("Plastic bottle scan saved", "Recyclable PET 1 classified at 0.2 kg", "10 May 2025", "success"),
        new("Doorstep pickup scheduled", "Recyclables collection booked for 12 May 2025", "09 May 2025", "info"),
        new("Reward points issued", "+10 eco points added to Alex Rivera", "08 May 2025", "success")
    ];

    public DashboardDto GetDashboard() => new(
        "Alex Rivera",
        [
            new("Reward Points", "1,200 pts", "+120 this month", "success"),
            new("Items Scanned", "48", "89% verified accuracy", "info"),
            new("Weight Diverted", "82.4 kg", "Away from landfill", "warning"),
            new("Scheduled Pickups", "2", "Next pickup: 12 May", "neutral")
        ],
        _activity,
        _pickups.Take(3));

    public IReadOnlyList<PickupDto> GetPickups(string? status = null)
    {
        if (string.IsNullOrWhiteSpace(status) || status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return _pickups;
        }

        return _pickups
            .Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public PickupDto CreatePickup(CreatePickupRequest request)
    {
        var pickup = new PickupDto(
            $"#LP-{Random.Shared.Next(9100, 9999)}",
            $"{request.PreferredDate:dd MMM yyyy}, {request.PreferredTimeWindow}",
            request.HomeAddress,
            request.WasteCategory,
            "Pending",
            "Scheduled");

        _pickups.Insert(0, pickup);
        _activity.Insert(0, new("Pickup request submitted", $"{request.WasteCategory} collection created for {request.PreferredDate:dd MMM yyyy}", "Today", "info"));
        return pickup;
    }

    public ScanResultDto ClassifyScan(string fileName)
    {
        var lower = fileName.ToLowerInvariant();
        var category = lower.Contains("glass") ? "Glass Bottles" : lower.Contains("paper") ? "Paper" : "Plastics (PET 1)";
        var item = category == "Paper" ? "Cardboard Sheet" : category == "Glass Bottles" ? "Glass Jar" : "Plastic Bottle";
        var points = category == "Glass Bottles" ? 14 : category == "Paper" ? 8 : 10;

        _activity.Insert(0, new($"{item} scan completed", $"{category} verified with high confidence", "Today", "success"));
        return new ScanResultDto(item, "Recyclable", category, "0.2 kg", points, 94, DateTimeOffset.Now);
    }

    public AdminStatsDto GetAdminStats() => new(
        [
            new("Total Active Users", "1,250", "+12% this week", "success"),
            new("Items Scanned", "320", "89% correct verification", "info"),
            new("Total Weight Diverted", "1,870 kg", "Circular output goal", "warning"),
            new("Reward Points Logged", "15,620", "92% redemption active", "danger")
        ],
        [
            new("Plastic", 45, "success"),
            new("Paper", 25, "info"),
            new("Glass", 20, "warning"),
            new("Metal", 10, "danger")
        ],
        _pickups);
}
