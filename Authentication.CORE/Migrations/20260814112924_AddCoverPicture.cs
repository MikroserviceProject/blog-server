using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.CORE.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverPicture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverPictureUrl",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverPictureUrl",
                table: "Users");
        }
    }
}
