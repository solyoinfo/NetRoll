using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetRoll.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CurrentPlan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanChangeRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeRequests_UserId_Status_CreatedUtc",
                table: "PlanChangeRequests",
                columns: new[] { "UserId", "Status", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanChangeRequests");
        }
    }
}
