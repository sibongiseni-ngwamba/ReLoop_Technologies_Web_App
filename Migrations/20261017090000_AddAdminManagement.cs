using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReLoop_Technologies_Web_App.Data;

#nullable disable

namespace ReLoop_Technologies_Web_App.Migrations
{
    [DbContext(typeof(ReLoopDbContext))]
    [Migration("20261017090000_AddAdminManagement")]
    public partial class AddAdminManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Address", table: "Users", type: "nvarchar(240)", maxLength: 240, nullable: false, defaultValue: "452 Eco Circular Ave, Suite 3B");
            migrationBuilder.AddColumn<string>(name: "PreferredCategory", table: "Users", type: "nvarchar(80)", maxLength: 80, nullable: false, defaultValue: "Recyclables");
            migrationBuilder.AddColumn<string>(name: "AssignedAgent", table: "Pickups", type: "nvarchar(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsActive", table: "Users", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), Email = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false), Subject = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false), Message = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false), Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false), CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false) },
                constraints: table => table.PrimaryKey("PK_ContactMessages", x => x.Id));
            migrationBuilder.CreateIndex(name: "IX_ContactMessages_Status_CreatedAt", table: "ContactMessages", columns: new[] { "Status", "CreatedAt" });
            migrationBuilder.CreateTable(
                name: "RewardCatalogue",
                columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false), PointsCost = table.Column<int>(type: "int", nullable: false), IsActive = table.Column<bool>(type: "bit", nullable: false), CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false) },
                constraints: table => table.PrimaryKey("PK_RewardCatalogue", x => x.Id));
            migrationBuilder.CreateTable(
                name: "RewardRedemptions",
                columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), RewardCatalogueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), PointsSpent = table.Column<int>(type: "int", nullable: false), Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false), CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false) },
                constraints: table => { table.PrimaryKey("PK_RewardRedemptions", x => x.Id); table.ForeignKey("FK_RewardRedemptions_Users_UserAccountId", x => x.UserAccountId, "Users", "Id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_RewardRedemptions_RewardCatalogue_RewardCatalogueItemId", x => x.RewardCatalogueItemId, "RewardCatalogue", "Id", onDelete: ReferentialAction.Restrict); });
            migrationBuilder.InsertData(
                table: "RewardCatalogue",
                columns: new[] { "Id", "Name", "Description", "PointsCost", "IsActive", "CreatedAt" },
                values: new object[,]
                {
                    { new Guid("ab0a3cb8-a41a-4b22-b541-d775f4f0b2d2"), "R50 partner voucher", "Redeemable at participating circular-economy partners.", 500, true, new DateTimeOffset(2025, 5, 1, 8, 0, 0, TimeSpan.Zero) },
                    { new Guid("cd0a3cb8-a41a-4b22-b541-d775f4f0b2d2"), "Tree planting contribution", "Fund a verified indigenous tree planting contribution.", 250, true, new DateTimeOffset(2025, 5, 1, 8, 0, 0, TimeSpan.Zero) }
                });
            migrationBuilder.CreateIndex(name: "IX_RewardRedemptions_UserAccountId", table: "RewardRedemptions", column: "UserAccountId");
            migrationBuilder.CreateIndex(name: "IX_RewardRedemptions_RewardCatalogueItemId", table: "RewardRedemptions", column: "RewardCatalogueItemId");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ContactMessages");
            migrationBuilder.DropTable(name: "RewardRedemptions");
            migrationBuilder.DropTable(name: "RewardCatalogue");
            migrationBuilder.DropColumn(name: "AssignedAgent", table: "Pickups");
            migrationBuilder.DropColumn(name: "IsActive", table: "Users");
            migrationBuilder.DropColumn(name: "Address", table: "Users");
            migrationBuilder.DropColumn(name: "PreferredCategory", table: "Users");
        }
    }
}
