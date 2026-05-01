using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace buronet_service.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostImageType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Posts");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProfilePictureMediaId",
                table: "UserProfiles",
                type: "uuid",
                nullable: true,
                
                oldClrType: typeof(string),
                oldType: "char(48)",
                oldMaxLength: 48,
                oldNullable: true)
                ;

            migrationBuilder.AddColumn<Guid>(
                name: "Image",
                table: "Posts",
                type: "uuid",
                nullable: true,
                collation: "ascii_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "Posts");

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureMediaId",
                table: "UserProfiles",
                type: "char(48)",
                maxLength: 48,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true)
                
                ;

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Posts",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                ;
        }
    }
}

