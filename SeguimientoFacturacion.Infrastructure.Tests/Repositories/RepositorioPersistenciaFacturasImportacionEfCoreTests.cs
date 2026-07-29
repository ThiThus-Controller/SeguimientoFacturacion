using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

/// <summary>
/// Pruebas del repositorio de persistencia definitiva
/// de pacientes y facturas importadas.
/// </summary>
public sealed class
    RepositorioPersistenciaFacturasImportacionEfCoreTests
{
    private static readonly DateTimeOffset FechaAuditoria =
        new(
            2026,
            7,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task
        ListarPacientes_DebeRespetarTipoYNumeroDocumento()
    {
        await using var contexto = CrearContexto();

        var pacienteEsperado =
            CrearPaciente(
                tipoDocumentoId: 1,
                numeroDocumento: "123456",
                nombreCompleto: "PACIENTE ESPERADO");

        var pacienteConOtroTipo =
            CrearPaciente(
                tipoDocumentoId: 2,
                numeroDocumento: "123456",
                nombreCompleto: "PACIENTE DE OTRO TIPO");

        var pacienteNoSolicitado =
            CrearPaciente(
                tipoDocumentoId: 1,
                numeroDocumento: "999999",
                nombreCompleto: "PACIENTE NO SOLICITADO");

        await contexto.Pacientes.AddRangeAsync(
            pacienteEsperado,
            pacienteConOtroTipo,
            pacienteNoSolicitado);

        await contexto.GuardarCambiosAsync();

        var repositorio =
            new
                RepositorioPersistenciaFacturasImportacionEfCore(
                    contexto);

        IdentificacionPacienteImportacionDto[]
            identificaciones =
            [
                new(
                    tipoDocumentoId: 1,
                    numeroDocumento: " 123456 "),
                new(
                    tipoDocumentoId: 3,
                    numeroDocumento: "123456"),
                new(
                    tipoDocumentoId: 1,
                    numeroDocumento: "000000")
            ];

        var resultado =
            await repositorio
                .ListarPacientesExistentesAsync(
                    identificaciones);

        var paciente = Assert.Single(resultado);

        Assert.Equal(
            pacienteEsperado.Id,
            paciente.Id);

        Assert.Equal(
            1,
            paciente.TipoDocumentoId);

        Assert.Equal(
            "123456",
            paciente.NumeroDocumento);
    }

    [Fact]
    public async Task
        ListarFacturas_DebeRetornarSoloIdentificadoresExistentes()
    {
        await using var contexto = CrearContexto();

        var paciente =
            CrearPaciente(
                tipoDocumentoId: 1,
                numeroDocumento: "123456",
                nombreCompleto: "PACIENTE DE PRUEBA");

        var factura =
            CrearFactura(
                numero: "000001",
                numeroDocumento: paciente.NumeroDocumento);

        await contexto.Pacientes.AddAsync(paciente);
        await contexto.Facturas.AddAsync(factura);
        await contexto.GuardarCambiosAsync();

        var repositorio =
            new
                RepositorioPersistenciaFacturasImportacionEfCore(
                    contexto);

        var resultado =
            await repositorio
                .ListarIdentificadoresFacturasExistentesAsync(
                    [
                        " fv000001 ",
                        "FV000002"
                    ]);

        var identificador = Assert.Single(resultado);

        Assert.Equal("FV000001", identificador);
    }

    [Fact]
    public async Task
        AgregarPacientesYFacturas_DebeMarcarlosComoAgregados()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaFacturasImportacionEfCore(
                    contexto);

        var paciente =
            CrearPaciente(
                tipoDocumentoId: 1,
                numeroDocumento: "123456",
                nombreCompleto: "PACIENTE NUEVO");

        var factura =
            CrearFactura(
                numero: "000001",
                numeroDocumento: paciente.NumeroDocumento);

        await repositorio.AgregarPacientesAsync(
            [paciente]);

        await repositorio.AgregarFacturasAsync(
            [factura]);

        Assert.Equal(
            EntityState.Added,
            contexto.Entry(paciente).State);

        Assert.Equal(
            EntityState.Added,
            contexto.Entry(factura).State);
    }

    [Fact]
    public async Task
        AgregarPacientes_ConIdentificacionDuplicada_DebeRechazar()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaFacturasImportacionEfCore(
                    contexto);

        Paciente[] pacientes =
        [
            CrearPaciente(
                tipoDocumentoId: 1,
                numeroDocumento: "123456",
                nombreCompleto: "PACIENTE UNO"),

            CrearPaciente(
                tipoDocumentoId: 1,
                numeroDocumento: "123456",
                nombreCompleto: "PACIENTE DOS")
        ];

        await Assert.ThrowsAsync<ArgumentException>(
            () => repositorio.AgregarPacientesAsync(
                pacientes));
    }

    [Fact]
    public async Task
        AgregarFacturas_ConIdentificadorDuplicado_DebeRechazar()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaFacturasImportacionEfCore(
                    contexto);

        Factura[] facturas =
        [
            CrearFactura(
                numero: "000001",
                numeroDocumento: "123456"),

            CrearFactura(
                numero: "000001",
                numeroDocumento: "999999")
        ];

        await Assert.ThrowsAsync<ArgumentException>(
            () => repositorio.AgregarFacturasAsync(
                facturas));
    }

    [Fact]
    public async Task
        Consultas_ConColeccionesVacias_DebenRetornarVacio()
    {
        await using var contexto = CrearContexto();

        var repositorio =
            new
                RepositorioPersistenciaFacturasImportacionEfCore(
                    contexto);

        var pacientes =
            await repositorio
                .ListarPacientesExistentesAsync([]);

        var facturas =
            await repositorio
                .ListarIdentificadoresFacturasExistentesAsync(
                    []);

        Assert.Empty(pacientes);
        Assert.Empty(facturas);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarRepositorio()
    {
        ServiceCollection services = new();

        var valoresConfiguracion =
            new Dictionary<string, string?>
            {
                [
                    $"ConnectionStrings:" +
                    $"{NombresConexion.Seguimiento}"
                ] =
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;"
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    valoresConfiguracion)
                .Build();

        services.AddInfrastructure(configuration);

        var descriptor =
            services.Single(
                elemento =>
                    elemento.ServiceType ==
                    typeof(
                        IRepositorioPersistenciaFacturasImportacion));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                RepositorioPersistenciaFacturasImportacionEfCore),
            descriptor.ImplementationType);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"SeguimientoDefinitivo_" +
                    $"{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static Paciente CrearPaciente(
        int tipoDocumentoId,
        string numeroDocumento,
        string nombreCompleto)
    {
        var paciente = new Paciente(
            tipoDocumentoId,
            numeroDocumento,
            nombreCompleto);

        paciente.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return paciente;
    }

    private static Factura CrearFactura(
        string numero,
        string numeroDocumento)
    {
        var factura = new Factura(
            prefijo: "FV",
            numero: numero,
            fechaFactura:
                new DateOnly(2026, 7, 15),
            aseguradoraId: 1,
            valor: 150000m,
            fechaRadicacion:
                new DateOnly(2026, 7, 20),
            tipoDocumentoId: 1,
            numeroDocumento: numeroDocumento,
            nombreCompleto:
                "PACIENTE DE PRUEBA",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: $"ADM{numero}",
            fechaAdmision:
                new DateOnly(2026, 7, 10),
            estadoId: 1,
            facturadorId: 1);

        factura.RegistrarCreacion(
            FechaAuditoria,
            "usuario-pruebas");

        return factura;
    }
}