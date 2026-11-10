using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ssw2026TermProject.data.migrations
{
    /// <inheritdoc />
    public partial class addedPostalCodeFieldToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Customers");
        }
    }
}
