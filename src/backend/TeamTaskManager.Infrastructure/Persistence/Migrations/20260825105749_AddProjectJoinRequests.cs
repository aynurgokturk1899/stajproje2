using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamTaskManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectJoinRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectJoinRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectJoinRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectJoinRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectJoinRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJoinRequests_ProjectId_Status",
                table: "ProjectJoinRequests",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJoinRequests_ReviewedByUserId",
                table: "ProjectJoinRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJoinRequests_UserId_Status",
                table: "ProjectJoinRequests",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectJoinRequests");
        }
    }
}
