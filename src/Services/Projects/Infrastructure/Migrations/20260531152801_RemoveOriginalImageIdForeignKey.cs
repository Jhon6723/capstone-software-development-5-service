using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixPro.Services.Projects.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOriginalImageIdForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_images_images_original_image_id",
                table: "images");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_images_images_original_image_id",
                table: "images",
                column: "original_image_id",
                principalTable: "images",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
