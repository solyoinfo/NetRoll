using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetRoll.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserUsageAndPlanName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserUsages",
                columns: table => new
                {
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileCount = table.Column<int>(type: "int", nullable: false),
                    StorageBytes = table.Column<long>(type: "bigint", nullable: false),
                    ProductCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUsages", x => x.OwnerUserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserUsages");

            migrationBuilder.DropColumn(
                name: "PlanName",
                table: "AspNetUsers");
        }
    }
}
