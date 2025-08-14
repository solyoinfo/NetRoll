using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetRoll.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImageAssetMeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "ImageAssets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ImageAssets",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AltText",
                table: "ImageAssets");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ImageAssets");
        }
    }
}
