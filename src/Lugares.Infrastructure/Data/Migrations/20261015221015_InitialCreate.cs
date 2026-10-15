using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lugares.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias_lugar",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_lugar", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "control_cuota_google",
                columns: table => new
                {
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    llamadas_realizadas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    limite_diario = table.Column<int>(type: "integer", nullable: false),
                    circuito_abierto = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fecha_actualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_cuota_google", x => x.fecha);
                });

            migrationBuilder.CreateTable(
                name: "lugares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    origen = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "curado_local"),
                    id_google_place = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tipo_lugar = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "punto_interes"),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    barrio = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subzona = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    latitud = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: false),
                    longitud = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    duracion_estimada_minutos = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)60),
                    nivel_precio = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    puntuacion = table.Column<decimal>(type: "numeric(2,1)", precision: 2, scale: 1, nullable: true),
                    cantidad_resenas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    es_bajo_techo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    es_al_aire_libre = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    apto_ninos = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    accesible_movilidad_reducida = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ofrece_menu_infantil = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_actualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lugares", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fotos_lugar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    orden = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    es_portada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fotos_lugar", x => x.id);
                    table.ForeignKey(
                        name: "FK_fotos_lugar_lugares_lugar_id",
                        column: x => x.lugar_id,
                        principalTable: "lugares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lugar_categorias",
                columns: table => new
                {
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<short>(type: "smallint", nullable: false),
                    es_principal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lugar_categorias", x => new { x.lugar_id, x.categoria_id });
                    table.ForeignKey(
                        name: "FK_lugar_categorias_categorias_lugar_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias_lugar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lugar_categorias_lugares_lugar_id",
                        column: x => x.lugar_id,
                        principalTable: "lugares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lugar_opciones_dietarias",
                columns: table => new
                {
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restriccion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lugar_opciones_dietarias", x => new { x.lugar_id, x.restriccion });
                    table.ForeignKey(
                        name: "FK_lugar_opciones_dietarias_lugares_lugar_id",
                        column: x => x.lugar_id,
                        principalTable: "lugares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resenas_lugar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origen = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "curado_local"),
                    autor_nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    puntuacion = table.Column<short>(type: "smallint", nullable: false),
                    comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fecha_resena = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resenas_lugar", x => x.id);
                    table.ForeignKey(
                        name: "FK_resenas_lugar_lugares_lugar_id",
                        column: x => x.lugar_id,
                        principalTable: "lugares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_lugar_codigo",
                table: "categorias_lugar",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fotos_lugar_lugar_id_orden",
                table: "fotos_lugar",
                columns: new[] { "lugar_id", "orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lugar_categorias_categoria_id",
                table: "lugar_categorias",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "idx_lugares_filtro_catalogo",
                table: "lugares",
                columns: new[] { "barrio", "tipo_lugar", "es_bajo_techo" });

            migrationBuilder.CreateIndex(
                name: "idx_lugares_geo",
                table: "lugares",
                columns: new[] { "barrio", "latitud", "longitud" });

            migrationBuilder.CreateIndex(
                name: "IX_lugares_id_google_place",
                table: "lugares",
                column: "id_google_place",
                unique: true,
                filter: "id_google_place IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_resenas_lugar_lugar_id",
                table: "resenas_lugar",
                column: "lugar_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "control_cuota_google");

            migrationBuilder.DropTable(
                name: "fotos_lugar");

            migrationBuilder.DropTable(
                name: "lugar_categorias");

            migrationBuilder.DropTable(
                name: "lugar_opciones_dietarias");

            migrationBuilder.DropTable(
                name: "resenas_lugar");

            migrationBuilder.DropTable(
                name: "categorias_lugar");

            migrationBuilder.DropTable(
                name: "lugares");
        }
    }
}
