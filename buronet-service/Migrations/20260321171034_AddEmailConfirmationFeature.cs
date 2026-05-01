using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace buronet_service.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmationFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PendingUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        ,
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        ,
                    PasswordHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    ConfirmationTokenHash = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        ,
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingUsers", x => x.Id);
                })
                ;

            migrationBuilder.CreateIndex(
                name: "IX_PendingUsers_Email",
                table: "PendingUsers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingUsers");

            migrationBuilder.DropColumn(
                name: "IsEmailConfirmed",
                table: "Users");
        }
    }
}

