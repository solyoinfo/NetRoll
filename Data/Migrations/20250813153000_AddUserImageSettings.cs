using Microsoft.EntityFrameworkCore.Migrations;
using NetRoll.Data;

namespace NetRoll.Data.Migrations
{
    [Migration("20250813153000_AddUserImageSettings")]
    public partial class AddUserImageSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserImageSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaxWidth = table.Column<int>(type: "int", nullable: false),
                    MaxHeight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserImageSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserImageSettings_OwnerUserId",
                table: "UserImageSettings",
                column: "OwnerUserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserImageSettings");
        }
    }
}
