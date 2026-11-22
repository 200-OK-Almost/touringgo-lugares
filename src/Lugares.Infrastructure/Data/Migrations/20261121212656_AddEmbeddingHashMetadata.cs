using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lugares.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmbeddingHashMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "embedding_source_hash",
                table: "lugares",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "embedding_updated_at",
                table: "lugares",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "embedding_source_hash",
                table: "lugares");

            migrationBuilder.DropColumn(
                name: "embedding_updated_at",
                table: "lugares");
        }
    }
}
