using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ReLoop_Technologies_Web_App.Migrations
{
    /// <inheritdoc />
    public partial class InitialReLoopSqlSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RewardPoints = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Tone = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pickups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ScheduledFor = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    WasteType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EstimatedWeightKg = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pickups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pickups_Users_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RewardLedger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardLedger_Users_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScanRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Disposition = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EstimatedWeightKg = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    PointsAwarded = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ScannedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanRecords_Users_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "RewardPoints", "Role" },
                values: new object[,]
                {
                    { new Guid("2a090ea4-dcf2-4327-a8b7-e2457092658e"), new DateTimeOffset(new DateTime(2025, 5, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "admin@reloop.co.za", "Mpho Admin", "demo-password-only", 0, "Admin" },
                    { new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f"), new DateTimeOffset(new DateTime(2025, 5, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "alex@example.com", "Alex Rivera", "demo-password-only", 1200, "Member" }
                });

            migrationBuilder.InsertData(
                table: "ActivityLogs",
                columns: new[] { "Id", "CreatedAt", "Detail", "Title", "Tone", "UserAccountId" },
                values: new object[,]
                {
                    { new Guid("30d55435-8355-44bb-a55e-7a9405490667"), new DateTimeOffset(new DateTime(2025, 5, 9, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Recyclables collection booked for 12 May 2025", "Doorstep pickup scheduled", "info", new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f") },
                    { new Guid("83588e8a-dc95-47bc-8d4c-e2dbff2f94c4"), new DateTimeOffset(new DateTime(2025, 5, 10, 10, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Recyclable PET 1 classified at 0.2 kg", "Plastic bottle scan saved", "success", new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f") },
                    { new Guid("86da6ff2-e9f4-4f0f-86cc-4caea9c8c080"), new DateTimeOffset(new DateTime(2025, 5, 8, 14, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "+10 eco points added to Alex Rivera", "Reward points issued", "success", new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f") }
                });

            migrationBuilder.InsertData(
                table: "Pickups",
                columns: new[] { "Id", "Address", "CreatedAt", "EstimatedWeightKg", "Notes", "ReferenceNumber", "ScheduledFor", "Status", "UserAccountId", "WasteType" },
                values: new object[,]
                {
                    { new Guid("8a151b65-8163-45ec-9fca-ae28f790d17c"), "452 Eco Circular Ave, Suite 3B", new DateTimeOffset(new DateTime(2025, 5, 7, 8, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4.5m, "Compost bag", "#LP-8931", new DateTimeOffset(new DateTime(2025, 5, 8, 14, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Completed", new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f"), "Organic" },
                    { new Guid("b3ec780d-50f3-4779-b786-f54b8b49af9a"), "710 Greenwood Terrace", new DateTimeOffset(new DateTime(2025, 4, 26, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Cancelled by user", "#LP-8521", new DateTimeOffset(new DateTime(2025, 4, 28, 10, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cancelled", new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f"), "E-waste" },
                    { new Guid("b5a53cb8-a41a-4b22-b541-d775f4f0b2d2"), "452 Eco Circular Ave, Suite 3B", new DateTimeOffset(new DateTime(2025, 5, 9, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Paper and plastics", "#LP-9082", new DateTimeOffset(new DateTime(2025, 5, 12, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Scheduled", new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f"), "Recyclables" },
                    { new Guid("d4a24c22-065b-47f6-a362-e7b168155ee0"), "452 Eco Circular Ave, Suite 3B", new DateTimeOffset(new DateTime(2025, 5, 4, 15, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m, "Plastic bottles", "#LP-8742", new DateTimeOffset(new DateTime(2025, 5, 5, 11, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Completed", new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f"), "Recyclables" }
                });

            migrationBuilder.InsertData(
                table: "RewardLedger",
                columns: new[] { "Id", "CreatedAt", "Points", "Reason", "UserAccountId" },
                values: new object[] { new Guid("1da8585b-e266-4d25-98b3-9e0206c5545b"), new DateTimeOffset(new DateTime(2025, 5, 10, 10, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 10, "Plastic Bottle scan verified", new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f") });

            migrationBuilder.InsertData(
                table: "ScanRecords",
                columns: new[] { "Id", "Category", "Confidence", "Disposition", "EstimatedWeightKg", "FileName", "ItemName", "PointsAwarded", "ScannedAt", "UserAccountId" },
                values: new object[] { new Guid("11a63bf5-e562-41fa-968a-b87c32095939"), "Plastics (PET 1)", 94, "Recyclable", 0.2m, "plastic-bottle.jpg", "Plastic Bottle", 10, new DateTimeOffset(new DateTime(2025, 5, 10, 10, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("2fc1f806-b68c-4a06-94fd-a97afcc24f2f") });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserAccountId_CreatedAt",
                table: "ActivityLogs",
                columns: new[] { "UserAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Pickups_ReferenceNumber",
                table: "Pickups",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pickups_UserAccountId_Status_ScheduledFor",
                table: "Pickups",
                columns: new[] { "UserAccountId", "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_RewardLedger_UserAccountId_CreatedAt",
                table: "RewardLedger",
                columns: new[] { "UserAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScanRecords_UserAccountId_ScannedAt",
                table: "ScanRecords",
                columns: new[] { "UserAccountId", "ScannedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "Pickups");

            migrationBuilder.DropTable(
                name: "RewardLedger");

            migrationBuilder.DropTable(
                name: "ScanRecords");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
