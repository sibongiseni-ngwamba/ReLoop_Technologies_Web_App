using Google.Api.Gax;
using Google.Cloud.Firestore;
using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Services;

const string expectedProjectId = "relooptechnologies-2eda2";
var projectId = ReadOption(args, "--project-id");
var apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
var acknowledged = args.Contains("--confirm-live-demo-data", StringComparer.OrdinalIgnoreCase);

if (projectId is null)
{
    Console.WriteLine("Usage: dotnet run --project tools/FirestoreDemoSeed -- --project-id relooptechnologies-2eda2 [--apply --confirm-live-demo-data]");
    Console.WriteLine("Without --apply this is a dry run. No Firestore records are written.");
    return 2;
}

if (!string.Equals(projectId, expectedProjectId, StringComparison.Ordinal))
{
    Console.Error.WriteLine($"Refusing project '{projectId}'. This seed is intentionally restricted to '{expectedProjectId}'.");
    return 2;
}

if (apply && !acknowledged)
{
    Console.Error.WriteLine("Live writes require both --apply and --confirm-live-demo-data.");
    return 2;
}

var records = DemoData.Build();
Console.WriteLine($"Firestore project: {projectId} (default database)");
Console.WriteLine($"Planned demo documents: {records.Count} across {records.Select(record => record.Collection).Distinct().Count()} ERD collections.");
foreach (var collection in records.GroupBy(record => record.Collection).OrderBy(group => group.Key))
    Console.WriteLine($"  {collection.Key}: {collection.Count()}");
Console.WriteLine("Document IDs use the demo-* prefix. No Firebase Auth users or administrator accounts are created.");

if (!apply)
{
    Console.WriteLine("Dry run only. Re-run with --apply --confirm-live-demo-data to write these records.");
    return 0;
}

var firestore = new FirestoreDbBuilder
{
    ProjectId = projectId,
    DatabaseId = "(default)",
    EmulatorDetection = EmulatorDetection.None
}.Build();

var created = 0;
var skipped = 0;
foreach (var record in records)
{
    var document = firestore.Collection(record.Collection).Document(record.Id);
    if ((await document.GetSnapshotAsync()).Exists)
    {
        skipped++;
        continue;
    }

    await document.CreateAsync(record.Value);
    created++;
}

Console.WriteLine($"Seed complete: created {created}, skipped {skipped} existing demo documents.");
return 0;

static string? ReadOption(string[] arguments, string option)
{
    var index = Array.FindIndex(arguments, item => item.Equals(option, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}

internal sealed record SeedRecord(string Collection, string Id, object Value);

internal static class DemoData
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly string[] UserIds = Enumerable.Range(1, 5).Select(index => $"demo-user-{index:000}").ToArray();
    private static readonly string[] AgentIds = Enumerable.Range(1, 5).Select(index => $"demo-agent-{index:000}").ToArray();
    private static readonly string[] PickupIds = Enumerable.Range(1, 5).Select(index => $"demo-pickup-{index:000}").ToArray();
    private static readonly string[] CategoryIds = Enumerable.Range(1, 5).Select(index => $"demo-category-{index:000}").ToArray();
    private static readonly string[] RewardItemIds = Enumerable.Range(1, 5).Select(index => $"demo-reward-item-{index:000}").ToArray();
    private static readonly string[] RedemptionIds = Enumerable.Range(1, 5).Select(index => $"demo-redemption-{index:000}").ToArray();

    public static List<SeedRecord> Build()
    {
        var categoryNames = new[] { "Plastic", "Paper", "Glass", "Metal", "Organic" };
        var users = Enumerable.Range(1, 5).Select(index => new UserDocument
        {
            UserId = UserIds[index - 1], FirstName = new[] { "Avery", "Jordan", "Sam", "Taylor", "Morgan" }[index - 1],
            LastName = "Demo", Email = $"demo.member{index}@example.invalid", PhoneNumber = $"+2700000000{index}",
            Address = $"{index} Demo Street, Rosebank, Johannesburg", Role = "Agent", IsActive = true,
            CreatedAt = Now.AddDays(-60 + index), UpdatedAt = Now.AddDays(-index), PreferredCategory = categoryNames[index - 1]
        }).ToArray();

        var agents = Enumerable.Range(1, 5).Select(index => new AgentDocument
        {
            AgentId = AgentIds[index - 1], UserId = UserIds[index - 1], Name = $"Demo Agent {index}",
            ContactNumber = $"+2700000010{index}", AssignedZone = new[] { "Rosebank", "Sandton", "Randburg", "Melrose", "Parkhurst" }[index - 1],
            IsAvailable = index != 5, IsActive = true, CreatedAt = Now.AddDays(-45 + index)
        }).ToArray();

        var pickups = Enumerable.Range(1, 5).Select(index => new PickupDocument
        {
            PickupId = PickupIds[index - 1], UserId = UserIds[index - 1], AgentId = AgentIds[index - 1],
            ScheduledDate = Now.AddDays(index == 4 ? -3 : index == 5 ? -2 : index), WasteType = categoryNames[index - 1], WasteTypes = [categoryNames[index - 1]],
            Address = users[index - 1].Address, Status = new[] { "Scheduled", "Assigned", "InProgress", "Completed", "Cancelled" }[index - 1],
            Notes = "Demo record for dashboard preview", ReferenceNumber = $"#DEMO-{index:000}",
            WeightKg = index == 4 ? 3.5 : null, CreatedAt = Now.AddDays(-index), UpdatedAt = Now,
            CompletedAt = index == 4 ? Now.AddDays(-1) : null, CancelledAt = index == 5 ? Now.AddDays(-1) : null,
            CancellationReason = index == 5 ? "Demo cancellation" : null
        }).ToArray();

        var categories = Enumerable.Range(1, 5).Select(index => new WasteCategoryDocument
        {
            WasteCategoryId = CategoryIds[index - 1], CategoryName = categoryNames[index - 1],
            Description = $"Demo guidance category: {categoryNames[index - 1]}.", ModelLabel = categoryNames[index - 1], IsActive = true,
            CreatedAt = Now.AddDays(-30 + index)
        }).ToArray();

        var rewardItems = Enumerable.Range(1, 5).Select(index => new RewardItemDocument
        {
            RewardItemId = RewardItemIds[index - 1], ItemName = new[] { "Reusable Tote", "Travel Mug", "Seedling Kit", "Bamboo Bottle", "Garden Compost" }[index - 1],
            Description = "Demo catalogue item; redeemable for points in the preview environment.", PointsCost = index * 100,
            IsActive = true, StockQuantity = 10 + index, CreatedAt = Now.AddDays(-20 + index), UpdatedAt = Now
        }).ToArray();

        var records = new List<SeedRecord>();
        records.AddRange(users.Select(item => new SeedRecord(FirestoreCollections.Users, item.UserId, item)));
        records.AddRange(agents.Select(item => new SeedRecord(FirestoreCollections.Agents, item.AgentId, item)));
        records.AddRange(pickups.Select(item => new SeedRecord(FirestoreCollections.Pickups, item.PickupId, item)));
        records.AddRange(Enumerable.Range(1, 5).Select(index =>
        {
            var item = new WasteLogDocument
            {
                WasteLogId = $"demo-waste-log-{index:000}", UserId = UserIds[index - 1], PickupId = PickupIds[index - 1],
                WasteCategoryId = CategoryIds[index - 1], WeightKg = 0.25 * index, LoggedAt = Now.AddDays(-index),
                Source = "demo", ModelLabel = categoryNames[index - 1], ConfidenceScore = 0.98, ItemName = $"Demo {categoryNames[index - 1]} item {index}",
                Disposition = "Recyclable", PointsAwarded = index * 10
            };
            return new SeedRecord(FirestoreCollections.WasteLogs, item.WasteLogId, item);
        }));
        records.AddRange(categories.Select(item => new SeedRecord(FirestoreCollections.WasteCategories, item.WasteCategoryId, item)));
        records.AddRange(Enumerable.Range(1, 5).Select(index =>
        {
            var item = new GuidanceContentDocument
            {
                GuidanceId = $"demo-guidance-{index:000}", WasteCategoryId = CategoryIds[index - 1],
                Title = $"Demo {categoryNames[index - 1]} recycling guide", Content = $"Rinse and sort demo {categoryNames[index - 1].ToLowerInvariant()} items before collection.",
                Preparation = "Clean, dry, and separate from general waste.", AcceptedItems = [$"Clean {categoryNames[index - 1]} container"],
                ExcludedItems = ["Heavily contaminated items"], IsActive = true, CreatedAt = Now.AddDays(-15 + index), UpdatedAt = Now
            };
            return new SeedRecord(FirestoreCollections.GuidanceContent, item.GuidanceId, item);
        }));
        records.AddRange(users.Select((user, index) =>
        {
            var item = new RewardAccountDocument
            {
                UserId = user.UserId, PointsBalance = 250 + index * 100, TotalEarned = 500 + index * 100,
                TotalRedeemed = 250, LastUpdated = Now.AddDays(-index)
            };
            return new SeedRecord(FirestoreCollections.Rewards, user.UserId, item);
        }));
        records.AddRange(rewardItems.Select(item => new SeedRecord(FirestoreCollections.RewardItems, item.RewardItemId, item)));
        records.AddRange(Enumerable.Range(1, 5).Select(index =>
        {
            var item = new RedemptionLogDocument
            {
                RedemptionId = RedemptionIds[index - 1], UserId = UserIds[index - 1], RewardItemId = RewardItemIds[index - 1],
                PointsUsed = index * 100, VoucherCode = $"DEMO-VOUCHER-{index:000}",
                Status = index == 4 ? "Completed" : "Pending", RedeemedAt = index == 4 ? Now.AddDays(-3) : null, CreatedAt = Now.AddDays(-4 + index)
            };
            return new SeedRecord(FirestoreCollections.RedemptionLogs, item.RedemptionId, item);
        }));
        records.AddRange(Enumerable.Range(1, 5).Select(index =>
        {
            var item = new PointsTransactionDocument
            {
                TransactionId = $"demo-points-transaction-{index:000}", UserId = UserIds[index - 1],
                TransactionType = "earn", Points = index * 25, RelatedId = $"demo-waste-log-{index:000}",
                Description = "Points awarded for demo recycling activity.", CreatedAt = Now.AddDays(-index)
            };
            return new SeedRecord(FirestoreCollections.PointsTransactions, item.TransactionId, item);
        }));
        records.AddRange(Enumerable.Range(1, 5).Select(index =>
        {
            var item = new NotificationDocument
            {
                NotificationId = $"demo-notification-{index:000}", UserId = UserIds[index - 1],
                Message = $"Demo pickup #{index:000} has a dashboard status update.", Type = "info", SentAt = Now.AddDays(-index),
                IsRead = index % 2 == 0, RelatedPickupId = PickupIds[index - 1], Title = "Demo recycling update", CreatedAt = Now.AddDays(-index)
            };
            return new SeedRecord(FirestoreCollections.Notifications, item.NotificationId, item);
        }));
        return records;
    }
}
