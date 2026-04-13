using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertificateSystem.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataScopeToRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataScope",
                table: "AspNetRoles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataScope",
                table: "AspNetRoles");
        }
    }
}
