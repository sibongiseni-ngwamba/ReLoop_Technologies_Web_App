using Microsoft.EntityFrameworkCore;
using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Data.Entities;
using ReLoop_Technologies_Web_App.Models;

namespace ReLoop_Technologies_Web_App.Services;

public sealed class ReLoopStore(ReLoopDbContext dbContext)
{
    private static readonly Guid DemoUserId = new("2fc1f806-b68c-4a06-94fd-a97afcc24f2f");

    public async Task<UserAccount> FindOrCreateUserAsync(string fullName, string email, string password)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(candidate => candidate.Email == email);
        if (user is not null)
        {
            return user;
        }

        user = new UserAccount
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = $"demo-{password.Length}-chars",
            Role = email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Member",
            Address = "452 Eco Circular Ave, Suite 3B",
            PreferredCategory = "Recyclables",
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<UserAccount?> GetUserByEmailAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return await GetDemoUserAsync();
        }

        return await dbContext.Users.FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task<ProfileDto> GetProfileAsync(string? email)
    {
        var user = await GetUserByEmailAsync(email) ?? await GetDemoUserAsync();
        var scanCount = await dbContext.ScanRecords.CountAsync(scan => scan.UserAccountId == user.Id);
        var pickupCount = await dbContext.Pickups.CountAsync(pickup => pickup.UserAccountId == user.Id);

        return new ProfileDto(
            user.FullName,
            user.Email,
            user.Role,
            user.Address,
            user.PreferredCategory,
            user.RewardPoints,
            scanCount,
            pickupCount);
    }

    public async Task<ProfileDto> UpdateProfileAsync(string? currentEmail, ProfileUpdateRequest request)
    {
        var user = await GetUserByEmailAsync(currentEmail) ?? await GetDemoUserAsync();
        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Address = request.Address;
        user.PreferredCategory = request.PreferredCategory;

        dbContext.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserAccountId = user.Id,
            Title = "Profile updated",
            Detail = "Account and recycling preferences were refreshed",
            Tone = "info",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return await GetProfileAsync(user.Email);
    }

    public async Task<ContactMessage> CreateContactMessageAsync(ContactRequest request)
    {
        var message = new ContactMessage
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            Subject = request.Subject,
            Message = request.Message,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.ContactMessages.Add(message);
        await dbContext.SaveChangesAsync();
        return message;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var user = await GetDemoUserAsync();
        var pickups = await dbContext.Pickups
            .Where(pickup => pickup.UserAccountId == user.Id)
            .OrderByDescending(pickup => pickup.ScheduledFor)
            .ToListAsync();
        var scans = await dbContext.ScanRecords.Where(scan => scan.UserAccountId == user.Id).ToListAsync();
        var activity = await dbContext.ActivityLogs
            .Where(log => log.UserAccountId == user.Id)
            .OrderByDescending(log => log.CreatedAt)
            .Take(6)
            .Select(log => new ActivityDto(log.Title, log.Detail, FormatDate(log.CreatedAt), log.Tone))
            .ToListAsync();
        var completedWeight = pickups.Where(pickup => pickup.EstimatedWeightKg.HasValue).Sum(pickup => pickup.EstimatedWeightKg!.Value);

        return new DashboardDto(
            user.FullName,
            [
                new("Reward Points", $"{user.RewardPoints:N0} pts", "+120 this month", "success"),
                new("Items Scanned", scans.Count.ToString("N0"), "94% latest confidence", "info"),
                new("Weight Diverted", $"{completedWeight:N1} kg", "Away from landfill", "warning"),
                new("Scheduled Pickups", pickups.Count(pickup => pickup.Status == "Scheduled").ToString("N0"), "Next pickup: 12 May", "neutral")
            ],
            activity,
            pickups.Take(3).Select(ToPickupDto));
    }

    public async Task<IReadOnlyList<PickupDto>> GetPickupsAsync(string? status = null)
    {
        var query = dbContext.Pickups.Where(pickup => pickup.UserAccountId == DemoUserId);
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(pickup => pickup.Status == status);
        }

        return await query
            .OrderByDescending(pickup => pickup.ScheduledFor)
            .Select(pickup => ToPickupDto(pickup))
            .ToListAsync();
    }

    public async Task<PickupDto> CreatePickupAsync(CreatePickupRequest request)
    {
        var pickup = new Pickup
        {
            Id = Guid.NewGuid(),
            UserAccountId = DemoUserId,
            ReferenceNumber = $"#LP-{Random.Shared.Next(9100, 9999)}",
            ScheduledFor = BuildPickupDate(request.PreferredDate, request.PreferredTimeWindow),
            Address = request.HomeAddress,
            WasteType = request.WasteCategory,
            Status = "Scheduled",
            Notes = request.Notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Pickups.Add(pickup);
        dbContext.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserAccountId = DemoUserId,
            Title = "Pickup request submitted",
            Detail = $"{request.WasteCategory} collection created for {request.PreferredDate:dd MMM yyyy}",
            Tone = "info",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return ToPickupDto(pickup);
    }

    public async Task<ScanResultDto> ClassifyScanAsync(string fileName)
    {
        var lower = fileName.ToLowerInvariant();
        var category = lower.Contains("glass") ? "Glass Bottles" : lower.Contains("paper") ? "Paper" : "Plastics (PET 1)";
        var item = category == "Paper" ? "Cardboard Sheet" : category == "Glass Bottles" ? "Glass Jar" : "Plastic Bottle";
        var points = category == "Glass Bottles" ? 14 : category == "Paper" ? 8 : 10;
        var result = new ScanRecord
        {
            Id = Guid.NewGuid(),
            UserAccountId = DemoUserId,
            ItemName = item,
            Disposition = "Recyclable",
            Category = category,
            EstimatedWeightKg = 0.2m,
            PointsAwarded = points,
            Confidence = 94,
            FileName = fileName,
            ScannedAt = DateTimeOffset.UtcNow
        };

        dbContext.ScanRecords.Add(result);
        dbContext.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserAccountId = DemoUserId,
            Title = $"{item} scan completed",
            Detail = $"{category} verified with high confidence",
            Tone = "success",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return ToScanDto(result);
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        var users = await dbContext.Users.CountAsync();
        var scans = await dbContext.ScanRecords.CountAsync();
        var totalWeight = await dbContext.Pickups.Where(pickup => pickup.EstimatedWeightKg.HasValue).SumAsync(pickup => pickup.EstimatedWeightKg!.Value);
        var rewards = await dbContext.RewardLedger.SumAsync(entry => entry.Points);
        var pickups = await dbContext.Pickups.OrderByDescending(pickup => pickup.CreatedAt).Take(8).ToListAsync();

        return new AdminStatsDto(
            [
                new("Total Active Users", users.ToString("N0"), "+12% this week", "success"),
                new("Items Scanned", scans.ToString("N0"), "94% latest confidence", "info"),
                new("Total Weight Diverted", $"{totalWeight:N1} kg", "Circular output goal", "warning"),
                new("Reward Points Logged", rewards.ToString("N0"), "Ledger total", "danger")
            ],
            [
                new("Plastic", 45, "success"),
                new("Paper", 25, "info"),
                new("Glass", 20, "warning"),
                new("Metal", 10, "danger")
            ],
            pickups.Select(ToPickupDto));
    }

    private async Task<UserAccount> GetDemoUserAsync() =>
        await dbContext.Users.FindAsync(DemoUserId)
        ?? throw new InvalidOperationException("Seed user was not found. Run EF Core migrations and database update.");

    private static PickupDto ToPickupDto(Pickup pickup) => new(
        pickup.ReferenceNumber,
        pickup.ScheduledFor.ToString("dd MMM yyyy, hh:mm tt"),
        pickup.Address,
        pickup.WasteType,
        pickup.EstimatedWeightKg.HasValue ? $"{pickup.EstimatedWeightKg:0.0} kg" : "Pending",
        pickup.Status);

    private static ScanResultDto ToScanDto(ScanRecord scan) => new(
        scan.ItemName,
        scan.Disposition,
        scan.Category,
        $"{scan.EstimatedWeightKg:0.0} kg",
        scan.PointsAwarded,
        scan.Confidence,
        scan.ScannedAt);

    private static string FormatDate(DateTimeOffset date) => date.Date == DateTimeOffset.UtcNow.Date ? "Today" : date.ToString("dd MMM yyyy");

    private static DateTimeOffset BuildPickupDate(DateTime preferredDate, string timeWindow)
    {
        var timeText = timeWindow.Split('-')[0].Trim();
        return DateTimeOffset.TryParse($"{preferredDate:yyyy-MM-dd} {timeText}", out var scheduled)
            ? scheduled
            : new DateTimeOffset(preferredDate);
    }
}
