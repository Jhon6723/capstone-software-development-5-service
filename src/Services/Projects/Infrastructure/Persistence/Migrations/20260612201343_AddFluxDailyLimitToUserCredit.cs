using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixPro.Services.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFluxDailyLimitToUserCredit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "flux_daily_count",
                table: "user_credits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "flux_daily_reset_at",
                table: "user_credits",
                type: "timestamptz",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "flux_daily_count",
                table: "user_credits");

            migrationBuilder.DropColumn(
                name: "flux_daily_reset_at",
                table: "user_credits");
        }
    }
}
