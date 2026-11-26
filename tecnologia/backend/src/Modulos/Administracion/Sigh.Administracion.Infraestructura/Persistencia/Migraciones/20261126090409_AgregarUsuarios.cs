using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigh.Administracion.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "administracion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_usuario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nombre_completo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    hash_contrasena = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cambio_contrasena_pendiente = table.Column<bool>(type: "boolean", nullable: false),
                    intentos_fallidos = table.Column<int>(type: "integer", nullable: false),
                    bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ultimo_acceso = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                    table.CheckConstraint("ck_usuarios_estado", "estado IN ('Activo','Desactivado')");
                });

            migrationBuilder.CreateTable(
                name: "usuario_roles",
                schema: "administracion",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_roles", x => new { x.usuario_id, x.rol });
                    table.CheckConstraint("ck_usuario_roles_rol", "rol IN ('Administrador','PersonalAdmision','EnfermeroTriaje','Medico','Farmaceutico','PersonalLaboratorio','Epidemiologo','Directivo')");
                    table.ForeignKey(
                        name: "FK_usuario_roles_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "administracion",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_nombre_usuario",
                schema: "administracion",
                table: "usuarios",
                column: "nombre_usuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuario_roles",
                schema: "administracion");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "administracion");
        }
    }
}
