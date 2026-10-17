using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLoop_Technologies_Web_App.Migrations
{
    public partial class AddAdminManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "AssignedAgent", table: "Pickups", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsActive", table: "Users", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.CreateTable(
                name: "RewardCatalogue",
                columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false), PointsCost = table.Column<int>(type: "int", nullable: false), IsActive = table.Column<bool>(type: "bit", nullable: false), CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false) },
                constraints: table => table.PrimaryKey("PK_RewardCatalogue", x => x.Id));
            migrationBuilder.CreateTable(
                name: "RewardRedemptions",
                columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), RewardCatalogueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), PointsSpent = table.Column<int>(type: "int", nullable: false), Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false), CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false) },
                constraints: table => { table.PrimaryKey("PK_RewardRedemptions", x => x.Id); table.ForeignKey("FK_RewardRedemptions_Users_UserAccountId", x => x.UserAccountId, "Users", "Id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_RewardRedemptions_RewardCatalogue_RewardCatalogueItemId", x => x.RewardCatalogueItemId, "RewardCatalogue", "Id", onDelete: ReferentialAction.Restrict); });
            migrationBuilder.CreateIndex(name: "IX_RewardRedemptions_UserAccountId", table: "RewardRedemptions", column: "UserAccountId");
            migrationBuilder.CreateIndex(name: "IX_RewardRedemptions_RewardCatalogueItemId", table: "RewardRedemptions", column: "RewardCatalogueItemId");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RewardRedemptions");
            migrationBuilder.DropTable(name: "RewardCatalogue");
            migrationBuilder.DropColumn(name: "AssignedAgent", table: "Pickups");
            migrationBuilder.DropColumn(name: "IsActive", table: "Users");
        }
    }
}
