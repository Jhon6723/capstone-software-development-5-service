using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixPro.Services.Projects.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_credits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_tier = table.Column<string>(type: "text", nullable: false),
                    credits_remaining = table.Column<int>(type: "integer", nullable: false),
                    credits_total = table.Column<int>(type: "integer", nullable: false),
                    subscription_tier = table.Column<string>(type: "text", nullable: false),
                    reset_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_credits", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_credits_user_id",
                table: "user_credits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_credits_user_id_model_tier",
                table: "user_credits",
                columns: new[] { "user_id", "model_tier" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_credits");
        }
    }
}
