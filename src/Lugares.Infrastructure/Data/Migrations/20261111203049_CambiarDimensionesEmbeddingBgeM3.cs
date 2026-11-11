using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Lugares.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CambiarDimensionesEmbeddingBgeM3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // pgvector no permite alterar la dimensión de una columna con índice;
            // se elimina el índice y se recrea la columna con la nueva dimensión.
            migrationBuilder.DropIndex(
                name: "IX_lugares_Embedding",
                table: "lugares");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "lugares");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "lugares",
                type: "vector(1024)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lugares_Embedding",
                table: "lugares",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lugares_Embedding",
                table: "lugares");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "lugares");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "lugares",
                type: "vector(384)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lugares_Embedding",
                table: "lugares",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }
    }
}
