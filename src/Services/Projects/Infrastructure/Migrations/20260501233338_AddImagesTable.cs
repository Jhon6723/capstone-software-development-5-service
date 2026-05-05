using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixPro.Services.Projects.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "varchar(255)", nullable: false),
                    content_type = table.Column<string>(type: "varchar(50)", nullable: false),
                    file_path = table.Column<string>(type: "varchar(500)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Pending"),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_images", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_images_owner_id",
                table: "images",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "images");
        }
    }
}
