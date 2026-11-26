using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sigh.Administracion.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarCatalogoRolesPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permisos",
                schema: "administracion",
                columns: table => new
                {
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permisos", x => x.codigo);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "administracion",
                columns: table => new
                {
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.codigo);
                });

            migrationBuilder.CreateTable(
                name: "rol_permisos",
                schema: "administracion",
                columns: table => new
                {
                    rol = table.Column<string>(type: "character varying(30)", nullable: false),
                    permiso = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol_permisos", x => new { x.rol, x.permiso });
                    table.ForeignKey(
                        name: "FK_rol_permisos_permisos_permiso",
                        column: x => x.permiso,
                        principalSchema: "administracion",
                        principalTable: "permisos",
                        principalColumn: "codigo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rol_permisos_roles_rol",
                        column: x => x.rol,
                        principalSchema: "administracion",
                        principalTable: "roles",
                        principalColumn: "codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "administracion",
                table: "permisos",
                columns: new[] { "codigo", "descripcion" },
                values: new object[,]
                {
                    { "bi.dashboards", "Consultar los dashboards de gestión" },
                    { "bitacora.consultar", "Consultar la bitácora de accesos" },
                    { "epidemiologia.reportes", "Consultar reportes y revisar alertas epidemiológicas" },
                    { "farmacia.dispensar", "Consultar las recetas pendientes y dispensar" },
                    { "farmacia.inventario", "Gestionar el inventario de medicamentos" },
                    { "hce.consultar", "Consultar la historia clínica completa" },
                    { "hce.recetas_examenes", "Emitir recetas y solicitar exámenes" },
                    { "hce.registrar_atencion", "Registrar y firmar una atención o una nota de corrección" },
                    { "laboratorio.resultados", "Consultar las órdenes y registrar los resultados de laboratorio" },
                    { "notificaciones.recibir", "Recibir notificaciones y gestionar la suscripción" },
                    { "pacientes.registrar", "Registrar y buscar pacientes, registrar el ingreso" },
                    { "triaje.registrar", "Registrar el triaje" },
                    { "usuarios.gestionar", "Gestionar usuarios, roles y permisos" }
                });

            migrationBuilder.InsertData(
                schema: "administracion",
                table: "roles",
                columns: new[] { "codigo", "descripcion", "nombre" },
                values: new object[,]
                {
                    { "Administrador", "Gestiona usuarios, roles y permisos, y supervisa el funcionamiento del sistema.", "Administrador" },
                    { "Directivo", "Consulta dashboards e indicadores de gestión.", "Directivo" },
                    { "EnfermeroTriaje", "Registra signos vitales y motivo de consulta, y clasifica el nivel de urgencia.", "Enfermero(a) de Triaje" },
                    { "Epidemiologo", "Consulta reportes epidemiológicos, revisa alertas tempranas y explora dashboards.", "Epidemiólogo" },
                    { "Farmaceutico", "Gestiona el inventario de medicamentos y dispensa las recetas.", "Farmacéutico" },
                    { "Medico", "Consulta la historia clínica, registra atenciones, emite recetas y solicita exámenes.", "Médico" },
                    { "PersonalAdmision", "Registra y busca pacientes, actualiza sus datos de filiación y registra el ingreso.", "Personal de Admisión" },
                    { "PersonalLaboratorio", "Recibe las órdenes de exámenes y registra sus resultados.", "Personal de Laboratorio" }
                });

            migrationBuilder.InsertData(
                schema: "administracion",
                table: "rol_permisos",
                columns: new[] { "permiso", "rol" },
                values: new object[,]
                {
                    { "bitacora.consultar", "Administrador" },
                    { "notificaciones.recibir", "Administrador" },
                    { "usuarios.gestionar", "Administrador" },
                    { "bi.dashboards", "Directivo" },
                    { "notificaciones.recibir", "Directivo" },
                    { "notificaciones.recibir", "EnfermeroTriaje" },
                    { "pacientes.registrar", "EnfermeroTriaje" },
                    { "triaje.registrar", "EnfermeroTriaje" },
                    { "bi.dashboards", "Epidemiologo" },
                    { "epidemiologia.reportes", "Epidemiologo" },
                    { "notificaciones.recibir", "Epidemiologo" },
                    { "farmacia.dispensar", "Farmaceutico" },
                    { "farmacia.inventario", "Farmaceutico" },
                    { "notificaciones.recibir", "Farmaceutico" },
                    { "hce.consultar", "Medico" },
                    { "hce.recetas_examenes", "Medico" },
                    { "hce.registrar_atencion", "Medico" },
                    { "notificaciones.recibir", "Medico" },
                    { "notificaciones.recibir", "PersonalAdmision" },
                    { "pacientes.registrar", "PersonalAdmision" },
                    { "laboratorio.resultados", "PersonalLaboratorio" },
                    { "notificaciones.recibir", "PersonalLaboratorio" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_rol_permisos_permiso",
                schema: "administracion",
                table: "rol_permisos",
                column: "permiso");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rol_permisos",
                schema: "administracion");

            migrationBuilder.DropTable(
                name: "permisos",
                schema: "administracion");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "administracion");
        }
    }
}
