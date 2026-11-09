using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Pgvector;

#nullable disable

namespace Lugares.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPgVectorAndPostGIS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "lugares",
                type: "vector(384)",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Ubicacion",
                table: "lugares",
                type: "geography (point, 4326)",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_lugares_Embedding",
                table: "lugares",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_lugares_Ubicacion",
                table: "lugares",
                column: "Ubicacion")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lugares_Embedding",
                table: "lugares");

            migrationBuilder.DropIndex(
                name: "IX_lugares_Ubicacion",
                table: "lugares");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "lugares");

            migrationBuilder.DropColumn(
                name: "Ubicacion",
                table: "lugares");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
