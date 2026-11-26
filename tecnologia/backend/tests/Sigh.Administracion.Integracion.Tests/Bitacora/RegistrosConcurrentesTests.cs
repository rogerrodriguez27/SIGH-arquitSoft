using Sigh.Administracion.Aplicacion.Publico;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Administracion.Infraestructura.Persistencia;
using Sigh.Administracion.Integracion.Tests.Soporte;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Integracion.Tests.Bitacora;

/// <summary>Caso límite de la spec: con muchos accesos simultáneos a la HCE, ningún registro se pierde.</summary>
[Collection(ColeccionPostgres.Nombre)]
[Trait("Category", "Integracion")]
public sealed class RegistrosConcurrentesTests(PostgresFixture bd)
{
    private sealed class HasherSimple : IHasherContrasenas
    {
        public string Calcular(string contrasena) => "hash:" + contrasena;

        public ResultadoVerificacionContrasena Verificar(string hash, string contrasena) =>
            ResultadoVerificacionContrasena.Incorrecta;
    }

    [Fact]
    public async Task Cien_registros_simultaneos_quedan_todos_guardados()
    {
        var medico = Usuario.Crear($"med{Guid.NewGuid():N}"[..20], "Médico Ficticio",
            ContrasenaPropuesta.Crear("Clave2026"), [CodigoRol.Medico], new HasherSimple(), new RelojSistema());
        await using (var contexto = bd.CrearContexto())
            await new RepositorioUsuariosEfCore(contexto).AgregarAsync(medico);
        var historia = Guid.NewGuid();

        await Task.WhenAll(Enumerable.Range(0, 100).Select(async i =>
        {
            await using var peticion = new PeticionSimulada(bd);
            var registro = new RegistroAcceso(medico.Id, "Medico", "CONSULTAR_HISTORIA", historia, "Hce");
            if (i % 2 == 0)
                await peticion.UnidadDeTrabajo.EjecutarEnTransaccionAsync(() => peticion.Bitacora.RegistrarAccesoAsync(registro));
            else
                await peticion.Bitacora.RegistrarIntentoDenegadoAsync(registro);
        }));

        await using var comando = bd.FuenteDatos.CreateCommand(
            "SELECT count(*) FROM administracion.bitacora_accesos_hce WHERE historia_id = $1");
        comando.Parameters.AddWithValue(historia);
        Assert.Equal(100L, await comando.ExecuteScalarAsync());
    }
}
