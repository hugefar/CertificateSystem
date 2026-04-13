using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CertificateSystem.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissionModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Code", "Description", "Name" },
                values: new object[,]
                {
                    { 1L, "系统首页", "Dashboard.View", "允许访问首页仪表盘", "查看首页" },
                    { 2L, "证书管理", "Certificate.Graduation", "毕业证书的查询和维护", "毕业证书管理" },
                    { 3L, "证书管理", "Certificate.Completion", "结业证书的查询和维护", "结业证书管理" },
                    { 4L, "证书管理", "Certificate.Degree", "学位证书的查询和维护", "学位证书管理" },
                    { 5L, "证书管理", "Certificate.SecondDegree", "第二学位证书的查询和维护", "第二学位证书管理" },
                    { 6L, "用户管理", "User.View", "查看用户列表", "查看用户" },
                    { 7L, "用户管理", "User.Manage", "新增、编辑、禁用用户", "管理用户" },
                    { 8L, "角色管理", "Role.View", "查看角色列表", "查看角色" },
                    { 9L, "角色管理", "Role.Manage", "新增、编辑、删除角色", "管理角色" },
                    { 10L, "角色管理", "Role.Permission", "为角色分配功能权限", "分配权限" },
                    { 11L, "安全日志", "SecurityLog.View", "查看安全审计日志", "查看安全日志" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Permissions");
        }
    }
}
