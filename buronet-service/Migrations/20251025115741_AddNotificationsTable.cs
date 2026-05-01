using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace buronet_service.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        ,
                    Message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        ,
                    Type = table.Column<string>(type: "text", nullable: false)
                        ,
                    RedirectUrl = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        ,
                    TargetId = table.Column<string>(type: "text", nullable: true)
                        ,
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                })
                ;

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}

