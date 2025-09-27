using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppProduct.Migrations
{
    /// <inheritdoc />
    public partial class AddProductModelField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Product",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Product SET Model = CASE Id
                    WHEN 1 THEN 'FBP-14'
                    WHEN 2 THEN 'SLU-13'
                    WHEN 3 THEN 'VPP-2025'
                    WHEN 4 THEN 'QPE-24'
                    WHEN 5 THEN 'SWP-X1'
                    WHEN 6 THEN 'GFB-17'
                    WHEN 7 THEN 'PCM-27'
                    WHEN 8 THEN 'TTP-12'
                    ELSE printf('PRD-%04d', Id)
                END
                WHERE Model IS NULL OR trim(Model) = ''
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Model",
                table: "Product");
        }
    }
}
