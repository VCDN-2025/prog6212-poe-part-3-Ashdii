using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMCSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimStatusColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionComments",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "RejectionComments",
                table: "Claims");
        }
    }
}
