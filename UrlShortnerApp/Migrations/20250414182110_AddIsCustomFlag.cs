﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortnerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCustomFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "Urls",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "Urls");
        }
    }
}
