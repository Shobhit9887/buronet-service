using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace buronet_service.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostImageFieldMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Image",
                table: "Posts",
                type: "text",
                maxLength: 2147483647,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true)
                
                ;
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Image",
                table: "Posts",
                type: "uuid",
                nullable: true,
                
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 2147483647,
                oldNullable: true)
                ;
        }
    }
}

