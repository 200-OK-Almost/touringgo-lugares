using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace Lugares.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AmpliarCatalogoLugaresGooglePlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = typeof(AmpliarCatalogoLugaresGooglePlaces).Assembly;
            var resourceName = "Lugares.Infrastructure.Data.Migrations.seed_google_places.sql";

            string? sql = null;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    sql = reader.ReadToEnd();
                }
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "Migrations", "seed_google_places.sql");
                if (File.Exists(filePath))
                {
                    sql = File.ReadAllText(filePath);
                }
            }

            if (!string.IsNullOrWhiteSpace(sql))
            {
                migrationBuilder.Sql(sql);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM public.lugares WHERE origen = 'google_places';");
        }
    }
}
