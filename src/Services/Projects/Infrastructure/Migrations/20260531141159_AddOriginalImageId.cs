using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixPro.Services.Projects.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalImageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "original_image_id",
                table: "images",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_images_original_image_id",
                table: "images",
                column: "original_image_id");

            migrationBuilder.AddForeignKey(
                name: "FK_images_images_original_image_id",
                table: "images",
                column: "original_image_id",
                principalTable: "images",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_images_images_original_image_id",
                table: "images");

            migrationBuilder.DropIndex(
                name: "IX_images_original_image_id",
                table: "images");

            migrationBuilder.DropColumn(
                name: "original_image_id",
                table: "images");
        }
    }
}
