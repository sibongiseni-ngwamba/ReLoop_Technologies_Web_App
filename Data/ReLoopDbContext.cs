using Microsoft.EntityFrameworkCore;
using ReLoop_Technologies_Web_App.Data.Entities;

namespace ReLoop_Technologies_Web_App.Data;

public sealed class ReLoopDbContext(DbContextOptions<ReLoopDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Pickup> Pickups => Set<Pickup>();
    public DbSet<ScanRecord> ScanRecords => Set<ScanRecord>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<RewardLedgerEntry> RewardLedger => Set<RewardLedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(180).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(40).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Pickup>(entity =>
        {
            entity.HasKey(pickup => pickup.Id);
            entity.Property(pickup => pickup.ReferenceNumber).HasMaxLength(24).IsRequired();
            entity.Property(pickup => pickup.Address).HasMaxLength(240).IsRequired();
            entity.Property(pickup => pickup.WasteType).HasMaxLength(80).IsRequired();
            entity.Property(pickup => pickup.Status).HasMaxLength(40).IsRequired();
            entity.Property(pickup => pickup.Notes).HasMaxLength(500);
            entity.Property(pickup => pickup.EstimatedWeightKg).HasPrecision(8, 2);
            entity.HasIndex(pickup => pickup.ReferenceNumber).IsUnique();
            entity.HasIndex(pickup => new { pickup.UserAccountId, pickup.Status, pickup.ScheduledFor });
            entity.HasOne(pickup => pickup.UserAccount)
                .WithMany(user => user.Pickups)
                .HasForeignKey(pickup => pickup.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScanRecord>(entity =>
        {
            entity.HasKey(scan => scan.Id);
            entity.Property(scan => scan.ItemName).HasMaxLength(120).IsRequired();
            entity.Property(scan => scan.Disposition).HasMaxLength(60).IsRequired();
            entity.Property(scan => scan.Category).HasMaxLength(100).IsRequired();
            entity.Property(scan => scan.FileName).HasMaxLength(180).IsRequired();
            entity.Property(scan => scan.EstimatedWeightKg).HasPrecision(8, 2);
            entity.HasIndex(scan => new { scan.UserAccountId, scan.ScannedAt });
            entity.HasOne(scan => scan.UserAccount)
                .WithMany(user => user.ScanRecords)
                .HasForeignKey(scan => scan.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(activity => activity.Id);
            entity.Property(activity => activity.Title).HasMaxLength(160).IsRequired();
            entity.Property(activity => activity.Detail).HasMaxLength(300).IsRequired();
            entity.Property(activity => activity.Tone).HasMaxLength(24).IsRequired();
            entity.HasIndex(activity => new { activity.UserAccountId, activity.CreatedAt });
            entity.HasOne(activity => activity.UserAccount)
                .WithMany(user => user.ActivityLogs)
                .HasForeignKey(activity => activity.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RewardLedgerEntry>(entity =>
        {
            entity.HasKey(reward => reward.Id);
            entity.Property(reward => reward.Reason).HasMaxLength(180).IsRequired();
            entity.HasIndex(reward => new { reward.UserAccountId, reward.CreatedAt });
            entity.HasOne(reward => reward.UserAccount)
                .WithMany(user => user.RewardLedger)
                .HasForeignKey(reward => reward.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        Seed(modelBuilder);
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        var userId = new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f");
        var adminId = new Guid("2a090ea4-dcf2-4327-a8b7-e2457092658e");

        modelBuilder.Entity<UserAccount>().HasData(
            new UserAccount { Id = userId, FullName = "Alex Rivera", Email = "alex@example.com", PasswordHash = "demo-password-only", Role = "Member", RewardPoints = 1200, CreatedAt = new DateTimeOffset(2025, 5, 1, 8, 0, 0, TimeSpan.Zero) },
            new UserAccount { Id = adminId, FullName = "Mpho Admin", Email = "admin@reloop.co.za", PasswordHash = "demo-password-only", Role = "Admin", RewardPoints = 0, CreatedAt = new DateTimeOffset(2025, 5, 1, 8, 0, 0, TimeSpan.Zero) });

        modelBuilder.Entity<Pickup>().HasData(
            new Pickup { Id = new Guid("b5a53cb8-a41a-4b22-b541-d775f4f0b2d2"), UserAccountId = userId, ReferenceNumber = "#LP-9082", ScheduledFor = new DateTimeOffset(2025, 5, 12, 9, 0, 0, TimeSpan.Zero), Address = "452 Eco Circular Ave, Suite 3B", WasteType = "Recyclables", EstimatedWeightKg = null, Status = "Scheduled", Notes = "Paper and plastics", CreatedAt = new DateTimeOffset(2025, 5, 9, 12, 0, 0, TimeSpan.Zero) },
            new Pickup { Id = new Guid("8a151b65-8163-45ec-9fca-ae28f790d17c"), UserAccountId = userId, ReferenceNumber = "#LP-8931", ScheduledFor = new DateTimeOffset(2025, 5, 8, 14, 30, 0, TimeSpan.Zero), Address = "452 Eco Circular Ave, Suite 3B", WasteType = "Organic", EstimatedWeightKg = 4.5m, Status = "Completed", Notes = "Compost bag", CreatedAt = new DateTimeOffset(2025, 5, 7, 8, 30, 0, TimeSpan.Zero) },
            new Pickup { Id = new Guid("d4a24c22-065b-47f6-a362-e7b168155ee0"), UserAccountId = userId, ReferenceNumber = "#LP-8742", ScheduledFor = new DateTimeOffset(2025, 5, 5, 11, 20, 0, TimeSpan.Zero), Address = "452 Eco Circular Ave, Suite 3B", WasteType = "Recyclables", EstimatedWeightKg = 1.2m, Status = "Completed", Notes = "Plastic bottles", CreatedAt = new DateTimeOffset(2025, 5, 4, 15, 0, 0, TimeSpan.Zero) },
            new Pickup { Id = new Guid("b3ec780d-50f3-4779-b786-f54b8b49af9a"), UserAccountId = userId, ReferenceNumber = "#LP-8521", ScheduledFor = new DateTimeOffset(2025, 4, 28, 10, 0, 0, TimeSpan.Zero), Address = "710 Greenwood Terrace", WasteType = "E-waste", EstimatedWeightKg = null, Status = "Cancelled", Notes = "Cancelled by user", CreatedAt = new DateTimeOffset(2025, 4, 26, 9, 0, 0, TimeSpan.Zero) });

        modelBuilder.Entity<ScanRecord>().HasData(
            new ScanRecord { Id = new Guid("11a63bf5-e562-41fa-968a-b87c32095939"), UserAccountId = userId, ItemName = "Plastic Bottle", Disposition = "Recyclable", Category = "Plastics (PET 1)", EstimatedWeightKg = 0.2m, PointsAwarded = 10, Confidence = 94, FileName = "plastic-bottle.jpg", ScannedAt = new DateTimeOffset(2025, 5, 10, 10, 15, 0, TimeSpan.Zero) });

        modelBuilder.Entity<ActivityLog>().HasData(
            new ActivityLog { Id = new Guid("83588e8a-dc95-47bc-8d4c-e2dbff2f94c4"), UserAccountId = userId, Title = "Plastic bottle scan saved", Detail = "Recyclable PET 1 classified at 0.2 kg", Tone = "success", CreatedAt = new DateTimeOffset(2025, 5, 10, 10, 15, 0, TimeSpan.Zero) },
            new ActivityLog { Id = new Guid("30d55435-8355-44bb-a55e-7a9405490667"), UserAccountId = userId, Title = "Doorstep pickup scheduled", Detail = "Recyclables collection booked for 12 May 2025", Tone = "info", CreatedAt = new DateTimeOffset(2025, 5, 9, 12, 0, 0, TimeSpan.Zero) },
            new ActivityLog { Id = new Guid("86da6ff2-e9f4-4f0f-86cc-4caea9c8c080"), UserAccountId = userId, Title = "Reward points issued", Detail = "+10 eco points added to Alex Rivera", Tone = "success", CreatedAt = new DateTimeOffset(2025, 5, 8, 14, 0, 0, TimeSpan.Zero) });

        modelBuilder.Entity<RewardLedgerEntry>().HasData(
            new RewardLedgerEntry { Id = new Guid("1da8585b-e266-4d25-98b3-9e0206c5545b"), UserAccountId = userId, Points = 10, Reason = "Plastic Bottle scan verified", CreatedAt = new DateTimeOffset(2025, 5, 10, 10, 15, 0, TimeSpan.Zero) });
    }
}
