using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosVemos.OrquestadorIA.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialIA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Analisis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Resolucion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Contexto = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BrilloPromedio = table.Column<double>(type: "float", nullable: false),
                    Contraste = table.Column<double>(type: "float", nullable: false),
                    NivelRiesgo = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Recomendacion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analisis", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Analisis");
        }
    }
}
