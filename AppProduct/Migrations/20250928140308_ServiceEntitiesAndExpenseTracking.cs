using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppProduct.Migrations
{
    /// <inheritdoc />
    public partial class ServiceEntitiesAndExpenseTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VendorName",
                table: "ServiceExpense",
                newName: "Vendor");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "ServiceExpense",
                newName: "ExpenseType");

            migrationBuilder.RenameColumn(
                name: "Receipt",
                table: "ServiceExpense",
                newName: "RejectionReason");

            migrationBuilder.RenameColumn(
                name: "IsApproved",
                table: "ServiceExpense",
                newName: "ApprovalStatus");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpenseDate",
                table: "ServiceExpense",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ReceiptUrl",
                table: "ServiceExpense",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptUrl",
                table: "ServiceExpense");

            migrationBuilder.RenameColumn(
                name: "Vendor",
                table: "ServiceExpense",
                newName: "VendorName");

            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "ServiceExpense",
                newName: "Receipt");

            migrationBuilder.RenameColumn(
                name: "ExpenseType",
                table: "ServiceExpense",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "ApprovalStatus",
                table: "ServiceExpense",
                newName: "IsApproved");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpenseDate",
                table: "ServiceExpense",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
