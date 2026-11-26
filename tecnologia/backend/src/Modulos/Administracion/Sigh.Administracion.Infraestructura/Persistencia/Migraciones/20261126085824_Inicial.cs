using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigh.Administracion.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "administracion");

            migrationBuilder.CreateTable(
                name: "eventos_seguridad",
                schema: "administracion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_hora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    objetivo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    detalle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_seguridad", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_eventos_seguridad_fecha_hora",
                schema: "administracion",
                table: "eventos_seguridad",
                column: "fecha_hora",
                descending: new bool[0]);

            // Inmutabilidad en la base de datos (FR-021, research D5): ningún UPDATE, DELETE ni TRUNCATE,
            // ni siquiera desde SQL manual. La función se reutiliza en la bitácora de accesos.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION administracion.bloquear_modificacion()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'La tabla %.% es de solo inserción: no se permite %',
                        TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP
                        USING ERRCODE = 'insufficient_privilege';
                END;
                $$;

                CREATE TRIGGER bloquear_modificacion
                    BEFORE UPDATE OR DELETE ON administracion.eventos_seguridad
                    FOR EACH ROW EXECUTE FUNCTION administracion.bloquear_modificacion();

                CREATE TRIGGER bloquear_truncado
                    BEFORE TRUNCATE ON administracion.eventos_seguridad
                    FOR EACH STATEMENT EXECUTE FUNCTION administracion.bloquear_modificacion();

                REVOKE UPDATE, DELETE, TRUNCATE ON administracion.eventos_seguridad FROM CURRENT_USER;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS administracion.bloquear_modificacion() CASCADE;");

            migrationBuilder.DropTable(
                name: "eventos_seguridad",
                schema: "administracion");
        }
    }
}
