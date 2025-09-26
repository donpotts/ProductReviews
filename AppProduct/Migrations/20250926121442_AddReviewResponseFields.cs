using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppProduct.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewResponseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Response",
                table: "ProductReview",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseDate",
                table: "ProductReview",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ResponseUserId",
                table: "ProductReview",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Response",
                table: "ProductReview");

            migrationBuilder.DropColumn(
                name: "ResponseDate",
                table: "ProductReview");

            migrationBuilder.DropColumn(
                name: "ResponseUserId",
                table: "ProductReview");
        }
    }
}
