using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppProduct.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReadDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActionUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notification");
        }
    }
}
