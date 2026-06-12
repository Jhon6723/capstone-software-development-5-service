using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixPro.Services.Projects.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateModelTierEnumValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM user_credits
                WHERE model_tier IN ('GptMiniLow', 'GptMiniHigh')
                  AND user_id IN (
                      SELECT user_id FROM user_credits WHERE model_tier = 'Kontext'
                  );

                UPDATE user_credits
                SET model_tier = 'Kontext'
                WHERE model_tier IN ('GptMiniLow', 'GptMiniHigh');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE user_credits
                SET model_tier = 'GptMiniLow'
                WHERE model_tier = 'Kontext';
            ");
        }
    }
}
