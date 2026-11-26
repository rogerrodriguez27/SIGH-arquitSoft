using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigh.Administracion.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarBitacoraAccesosHce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bitacora_accesos_hce",
                schema: "administracion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_usuario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha_hora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    historia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modulo_origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resultado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bitacora_accesos_hce", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bitacora_accesos_hce_fecha_hora",
                schema: "administracion",
                table: "bitacora_accesos_hce",
                column: "fecha_hora",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_bitacora_accesos_hce_historia_fecha_hora",
                schema: "administracion",
                table: "bitacora_accesos_hce",
                columns: new[] { "historia_id", "fecha_hora" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_bitacora_accesos_hce_usuario_fecha_hora",
                schema: "administracion",
                table: "bitacora_accesos_hce",
                columns: new[] { "usuario_id", "fecha_hora" },
                descending: new[] { false, true });

            // Inmutabilidad en la base de datos (FR-021, research D5), con la función de la migración Inicial.
            migrationBuilder.Sql("""
                CREATE TRIGGER bloquear_modificacion
                    BEFORE UPDATE OR DELETE OR TRUNCATE ON administracion.bitacora_accesos_hce
                    FOR EACH STATEMENT EXECUTE FUNCTION administracion.bloquear_modificacion();

                REVOKE UPDATE, DELETE, TRUNCATE ON administracion.bitacora_accesos_hce FROM CURRENT_USER;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bitacora_accesos_hce",
                schema: "administracion");
        }
    }
}
