using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace buoi2.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsForNewUser",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderAmount",
                table: "Vouchers",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "IsForNewUser",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MinOrderAmount",
                table: "Vouchers");
        }
    }
}
