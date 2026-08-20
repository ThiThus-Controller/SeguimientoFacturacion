using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Pagos;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioGestionManualPagosTests
{
    private static readonly DateTimeOffset FechaPrueba =
        new(2026, 8, 19, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ObtenerFactura_DebeNormalizarIdentificador()
    {
        var repositorio = CrearRepositorio();
        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultado = await servicio.ObtenerFacturaAsync(" fe100 ");

        Assert.NotNull(resultado);
        Assert.Equal("FE100", resultado.FacturaId);
    }

    [Fact]
    public async Task ObtenerHistorial_DebeNormalizarIdentificador()
    {
        var repositorio = CrearRepositorio();
        repositorio.Historial.Add(
            new PagoHistorialFacturaDto
            {
                PagoId = Guid.NewGuid(),
                AplicacionId = Guid.NewGuid(),
                FacturaId = "FE100",
                FechaPago = new DateOnly(2026, 8, 10),
                Recibo = "REC-HISTORICO",
                ValorTotalRecibo = 500m,
                ValorRecibidoFactura = 500m,
                ValorAplicado = 450m,
                ValorAnticipo = 50m,
                RetencionRecibo = decimal.Zero,
                ReteIcaRecibo = decimal.Zero,
                FechaCreacionUtc = FechaPrueba,
                CreadoPor = "importador"
            });
        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultado = await servicio
            .ObtenerHistorialPorFacturaAsync(" fe100 ");

        var pago = Assert.Single(resultado);
        Assert.Equal("REC-HISTORICO", pago.Recibo);
        Assert.Equal("FE100", repositorio.UltimaFacturaHistorial);
    }

    [Fact]
    public async Task Crear_ConExcedente_DebeAplicarSaldoYCrearAnticipo()
    {
        var repositorio = CrearRepositorio();
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.CrearAsync(
            CrearSolicitud(1200m),
            " operador-pagos ");

        Assert.Equal("REC-001", resultado.Recibo);
        Assert.Equal(1200m, resultado.ValorPagado);
        Assert.Equal(1000m, resultado.TotalAplicado);
        Assert.Equal(200m, resultado.TotalAnticipo);
        Assert.Equal("operador-pagos", resultado.CreadoPor);

        var aplicacion = Assert.Single(resultado.Aplicaciones);
        Assert.Equal("FE100", aplicacion.FacturaId);
        Assert.Equal(1000m, aplicacion.ValorAplicado);
        Assert.Equal(200m, aplicacion.ValorAnticipo);
        Assert.Equal(1000m, aplicacion.SaldoAntes);
        Assert.Equal(decimal.Zero, aplicacion.SaldoDespues);

        var pago = Assert.Single(repositorio.Pagos);
        Assert.Single(pago.Aplicaciones);
        Assert.Single(repositorio.Auditorias);
        Assert.Equal(1, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task Crear_FacturaConNotasYPagos_DebeUsarSaldoReal()
    {
        var repositorio = CrearRepositorio();
        repositorio.Referencias[0] = repositorio.Referencias[0] with
        {
            TotalNotasCredito = 300m,
            TotalNotasDebito = 100m,
            TotalPagosAplicados = 250m
        };
        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultado = await servicio.CrearAsync(
            CrearSolicitud(700m),
            "administrador");

        var aplicacion = Assert.Single(resultado.Aplicaciones);
        Assert.Equal(550m, aplicacion.SaldoAntes);
        Assert.Equal(550m, aplicacion.ValorAplicado);
        Assert.Equal(150m, aplicacion.ValorAnticipo);
        Assert.Equal(decimal.Zero, aplicacion.SaldoDespues);
    }

    [Fact]
    public async Task Crear_FacturaAnulada_DebeConvertirTodoEnAnticipo()
    {
        var repositorio = CrearRepositorio();
        repositorio.Referencias[0] = repositorio.Referencias[0] with
        {
            EstadoId = 3
        };
        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultado = await servicio.CrearAsync(
            CrearSolicitud(400m),
            "administrador");

        var aplicacion = Assert.Single(resultado.Aplicaciones);
        Assert.Equal(decimal.Zero, aplicacion.ValorAplicado);
        Assert.Equal(400m, aplicacion.ValorAnticipo);
        Assert.Equal(1000m, aplicacion.SaldoDespues);
    }

    [Fact]
    public async Task Crear_ReciboDuplicado_DebeBloquearSinGuardar()
    {
        var repositorio = CrearRepositorio();
        repositorio.ExistePago = true;
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.CrearAsync(
                CrearSolicitud(500m),
                "administrador"));

        Assert.Empty(repositorio.Pagos);
        Assert.Empty(repositorio.Auditorias);
        Assert.Equal(0, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task Crear_AseguradoraNoCoincide_DebeBloquear()
    {
        var repositorio = CrearRepositorio();
        repositorio.Referencias[0] = repositorio.Referencias[0] with
        {
            AseguradoraId = 2
        };
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.CrearAsync(
                CrearSolicitud(500m),
                "administrador"));

        Assert.Empty(repositorio.Pagos);
        Assert.Equal(0, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task Crear_FacturaInexistente_DebeBloquear()
    {
        var repositorio = CrearRepositorio();
        repositorio.Referencias.Clear();
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => servicio.CrearAsync(
                CrearSolicitud(500m),
                "administrador"));

        Assert.Empty(repositorio.Pagos);
        Assert.Equal(0, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task RevertirAplicacion_DebeReclasificarComoAnticipo()
    {
        var repositorio = CrearRepositorioConPago(800m, 800m, 0m);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);
        var pago = Assert.Single(repositorio.Pagos);
        var aplicacion = Assert.Single(pago.Aplicaciones);

        await servicio.RevertirAplicacionAsync(
            new SolicitudReversionAplicacionPagoDto
            {
                PagoId = pago.Id,
                AplicacionId = aplicacion.Id,
                Motivo = "Pago aplicado a la factura incorrecta."
            },
            "auditor");

        Assert.Equal(0m, aplicacion.ValorAplicado);
        Assert.Equal(800m, aplicacion.ValorAnticipo);
        Assert.Equal(800m, pago.TotalRecibidoDistribuido);
        Assert.Equal(
            TipoOperacionAuditoria.Reversion,
            Assert.Single(repositorio.Auditorias).TipoOperacion);
        Assert.Equal(1, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task RevertirAplicacion_SinValorAplicado_DebeBloquear()
    {
        var repositorio = CrearRepositorioConPago(500m, 0m, 500m);
        var servicio = CrearServicio(repositorio, new UnidadTrabajoFalsa());
        var pago = Assert.Single(repositorio.Pagos);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.RevertirAplicacionAsync(
                new SolicitudReversionAplicacionPagoDto
                {
                    PagoId = pago.Id,
                    AplicacionId = pago.Aplicaciones.Single().Id,
                    Motivo = "Reversión inválida."
                },
                "auditor"));
    }

    [Fact]
    public async Task AplicarAnticipo_MismaFactura_DebeConservarTotal()
    {
        var repositorio = CrearRepositorioConPago(500m, 200m, 300m);
        repositorio.Referencias[0] = repositorio.Referencias[0] with
        {
            TotalPagosAplicados = 200m
        };
        var servicio = CrearServicio(repositorio, new UnidadTrabajoFalsa());
        var pago = Assert.Single(repositorio.Pagos);
        var aplicacion = Assert.Single(pago.Aplicaciones);

        await servicio.AplicarAnticipoAsync(
            new SolicitudAplicacionAnticipoDto
            {
                PagoId = pago.Id,
                AplicacionOrigenId = aplicacion.Id,
                FacturaDestinoId = " fe100 ",
                Valor = 250m,
                Motivo = "Aplicación por saldo habilitado."
            },
            "tesoreria");

        Assert.Equal(450m, aplicacion.ValorAplicado);
        Assert.Equal(50m, aplicacion.ValorAnticipo);
        Assert.Equal(500m, pago.TotalRecibidoDistribuido);
    }

    [Fact]
    public async Task AplicarAnticipo_OtraFactura_DebeTransferirDistribucion()
    {
        var repositorio = CrearRepositorioConPago(500m, 0m, 500m);
        repositorio.Referencias.Add(
            repositorio.Referencias[0] with
            {
                FacturaId = "FE200",
                ValorFactura = 700m
            });
        var servicio = CrearServicio(repositorio, new UnidadTrabajoFalsa());
        var pago = Assert.Single(repositorio.Pagos);
        var origen = Assert.Single(pago.Aplicaciones);

        await servicio.AplicarAnticipoAsync(
            new SolicitudAplicacionAnticipoDto
            {
                PagoId = pago.Id,
                AplicacionOrigenId = origen.Id,
                FacturaDestinoId = "FE200",
                Valor = 500m,
                Motivo = "Cruce autorizado de anticipo."
            },
            "tesoreria");

        var destino = Assert.Single(pago.Aplicaciones);
        Assert.Equal("FE200", destino.FacturaId);
        Assert.Equal(500m, destino.ValorAplicado);
        Assert.Contains(origen, repositorio.AplicacionesEliminadas);
        Assert.Equal(500m, pago.TotalRecibidoDistribuido);
    }

    [Fact]
    public async Task AplicarAnticipo_OtraAseguradora_DebeBloquear()
    {
        var repositorio = CrearRepositorioConPago(500m, 0m, 500m);
        repositorio.Referencias.Add(
            repositorio.Referencias[0] with
            {
                FacturaId = "FE200",
                AseguradoraId = 2
            });
        var servicio = CrearServicio(repositorio, new UnidadTrabajoFalsa());
        var pago = Assert.Single(repositorio.Pagos);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AplicarAnticipoAsync(
                new SolicitudAplicacionAnticipoDto
                {
                    PagoId = pago.Id,
                    AplicacionOrigenId = pago.Aplicaciones.Single().Id,
                    FacturaDestinoId = "FE200",
                    Valor = 100m,
                    Motivo = "Intento entre aseguradoras."
                },
                "tesoreria"));
    }

    [Fact]
    public async Task AplicarAnticipoEntidad_DebeConsumirFuentesFifo()
    {
        var repositorio = CrearRepositorio();
        repositorio.Referencias.Add(
            repositorio.Referencias[0] with
            {
                FacturaId = "FE200",
                ValorFactura = 900m
            });
        var pagoAntiguo = CrearPagoConAnticipo(
            "REC-ANTIGUO",
            new DateOnly(2026, 8, 1),
            500m);
        var pagoReciente = CrearPagoConAnticipo(
            "REC-RECIENTE",
            new DateOnly(2026, 8, 2),
            1000m);
        repositorio.Pagos.AddRange(pagoReciente, pagoAntiguo);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        var resultado = await servicio.AplicarAnticipoEntidadAsync(
            new SolicitudAplicacionAnticipoEntidadDto
            {
                AseguradoraId = 1,
                FacturaDestinoId = " fe200 ",
                Valor = 900m,
                Motivo = "Cruce consolidado autorizado."
            },
            "tesoreria");

        Assert.Equal(900m, resultado.ValorAplicado);
        Assert.Equal(decimal.Zero, resultado.SaldoPosterior);
        Assert.Equal(600m, resultado.AnticipoDisponiblePosterior);
        Assert.Equal(2, resultado.FuentesConsumidas);
        Assert.Equal(
            500m,
            pagoAntiguo.Aplicaciones.Single(
                aplicacion => aplicacion.FacturaId == "FE200")
                .ValorAplicado);
        Assert.Equal(
            400m,
            pagoReciente.Aplicaciones.Single(
                aplicacion => aplicacion.FacturaId == "FE200")
                .ValorAplicado);
        Assert.Equal(
            600m,
            pagoReciente.Aplicaciones.Single(
                aplicacion => aplicacion.FacturaId == "FE100")
                .ValorAnticipo);
        Assert.Equal(2, repositorio.Auditorias.Count);
        Assert.Single(
            repositorio.Auditorias
                .Select(auditoria => auditoria.CorrelacionId)
                .Distinct());
        Assert.Equal(1, unidadTrabajo.Guardados);
    }

    [Fact]
    public async Task AplicarAnticipoEntidad_SaldoInsuficiente_DebeBloquear()
    {
        var repositorio = CrearRepositorioConPago(1500m, 0m, 1500m);
        repositorio.Referencias[0] = repositorio.Referencias[0] with
        {
            ValorFactura = 900m
        };
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var servicio = CrearServicio(repositorio, unidadTrabajo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AplicarAnticipoEntidadAsync(
                new SolicitudAplicacionAnticipoEntidadDto
                {
                    AseguradoraId = 1,
                    FacturaDestinoId = "FE100",
                    Valor = 901m,
                    Motivo = "Valor superior al saldo."
                },
                "tesoreria"));

        Assert.Equal(1500m, repositorio.Pagos.Single().TotalAnticipo);
        Assert.Empty(repositorio.Auditorias);
        Assert.Equal(0, unidadTrabajo.Guardados);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarServicio()
    {
        ServiceCollection servicios = new();

        servicios.AddApplication();

        var descriptor = servicios.Single(
            elemento => elemento.ServiceType ==
                typeof(SeguimientoFacturacion.Application
                    .Interfaces.Services.IServicioGestionManualPagos));

        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        Assert.Equal(
            typeof(ServicioGestionManualPagos),
            descriptor.ImplementationType);
    }

    private static SolicitudCreacionPagoManualDto CrearSolicitud(
        decimal valor)
    {
        return new SolicitudCreacionPagoManualDto
        {
            AseguradoraId = 1,
            FechaPago = new DateOnly(2026, 8, 10),
            Recibo = " rec-001 ",
            ValorPagado = valor,
            Retencion = 10m,
            ReteIca = 5m,
            Notas = " Pago manual de prueba. ",
            Aplicaciones =
            [
                new SolicitudAplicacionPagoManualDto
                {
                    FacturaId = " fe100 ",
                    ValorRecibido = valor
                }
            ]
        };
    }

    private static RepositorioFalso CrearRepositorio()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Referencias.Add(
            new FacturaReferenciaPagoManualDto
            {
                FacturaId = "FE100",
                AseguradoraId = 1,
                FechaFactura = new DateOnly(2026, 8, 1),
                EstadoId = 1,
                ValorFactura = 1000m,
                TotalNotasCredito = decimal.Zero,
                TotalNotasDebito = decimal.Zero,
                TotalPagosAplicados = decimal.Zero
            });

        return repositorio;
    }

    private static RepositorioFalso CrearRepositorioConPago(
        decimal recibido,
        decimal aplicado,
        decimal anticipo)
    {
        var repositorio = CrearRepositorio();
        var pago = new Pago(
            1,
            new DateOnly(2026, 8, 10),
            "REC-GESTION",
            recibido,
            0m,
            0m);
        var aplicacion = new AplicacionPago(
            pago.Id,
            "FE100",
            recibido,
            aplicado,
            anticipo);
        pago.AgregarAplicacion(aplicacion);
        pago.RegistrarCreacion(FechaPrueba, "importador");
        aplicacion.RegistrarCreacion(FechaPrueba, "importador");
        repositorio.Pagos.Add(pago);
        return repositorio;
    }

    private static Pago CrearPagoConAnticipo(
        string recibo,
        DateOnly fecha,
        decimal valor)
    {
        var pago = new Pago(1, fecha, recibo, valor, 0m, 0m);
        var aplicacion = new AplicacionPago(
            pago.Id,
            "FE100",
            valor,
            0m,
            valor);
        pago.AgregarAplicacion(aplicacion);
        pago.RegistrarCreacion(FechaPrueba, "importador");
        aplicacion.RegistrarCreacion(FechaPrueba, "importador");
        return pago;
    }

    private static ServicioGestionManualPagos CrearServicio(
        RepositorioFalso repositorio,
        UnidadTrabajoFalsa unidadTrabajo)
    {
        return new ServicioGestionManualPagos(
            repositorio,
            unidadTrabajo,
            new EjecutorTransaccionFalso(),
            new SolicitudCreacionPagoManualDtoValidator(),
            new SolicitudReversionAplicacionPagoDtoValidator(),
            new SolicitudAplicacionAnticipoDtoValidator(),
            new SolicitudAplicacionAnticipoEntidadDtoValidator(),
            new CalculadoraDistribucionPago(),
            new TimeProviderFalso(FechaPrueba));
    }

    private sealed class RepositorioFalso :
        IRepositorioGestionManualPagos
    {
        public bool ExistePago { get; set; }

        public List<FacturaReferenciaPagoManualDto> Referencias
            { get; } = [];

        public List<Pago> Pagos { get; } = [];
        public List<RegistroAuditoria> Auditorias { get; } = [];
        public List<AplicacionPago> AplicacionesEliminadas { get; } = [];
        public List<PagoHistorialFacturaDto> Historial { get; } = [];
        public string? UltimaFacturaHistorial { get; private set; }

        public Task<IReadOnlyList<FacturaReferenciaPagoManualDto>>
            ObtenerFacturasAsync(
                IReadOnlyCollection<string> facturaIds,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<FacturaReferenciaPagoManualDto> resultado =
                Referencias
                    .Where(referencia =>
                        facturaIds.Contains(
                            referencia.FacturaId,
                            StringComparer.OrdinalIgnoreCase))
                    .ToArray();

            return Task.FromResult(resultado);
        }

        public Task<IReadOnlyList<PagoHistorialFacturaDto>>
            ObtenerHistorialPorFacturaAsync(
                string facturaId,
                CancellationToken cancellationToken = default)
        {
            UltimaFacturaHistorial = facturaId;
            IReadOnlyList<PagoHistorialFacturaDto> resultado =
                Historial
                    .Where(pago => string.Equals(
                        pago.FacturaId,
                        facturaId,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();

            return Task.FromResult(resultado);
        }

        public Task<bool> ExisteAsync(
            int aseguradoraId,
            string recibo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistePago);

        public Task<Pago?> ObtenerParaGestionAsync(
            Guid pagoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Pagos.SingleOrDefault(pago => pago.Id == pagoId));

        public Task<IReadOnlyList<Pago>>
            ObtenerAnticiposEntidadParaGestionAsync(
                int aseguradoraId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Pago> resultado = Pagos
                .Where(pago =>
                    pago.AseguradoraId == aseguradoraId &&
                    pago.TotalAnticipo > decimal.Zero)
                .OrderBy(pago => pago.FechaPago)
                .ThenBy(pago => pago.Recibo)
                .ToArray();

            return Task.FromResult(resultado);
        }

        public void EliminarAplicacion(AplicacionPago aplicacion)
        {
            AplicacionesEliminadas.Add(aplicacion);
        }

        public Task AgregarAsync(
            Pago pago,
            CancellationToken cancellationToken = default)
        {
            Pagos.Add(pago);
            return Task.CompletedTask;
        }

        public Task AgregarAuditoriaAsync(
            RegistroAuditoria registro,
            CancellationToken cancellationToken = default)
        {
            Auditorias.Add(registro);
            return Task.CompletedTask;
        }
    }

    private sealed class UnidadTrabajoFalsa : IUnidadTrabajo
    {
        public int Guardados { get; private set; }

        public Task<int> GuardarCambiosAsync(
            CancellationToken cancellationToken = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }
    }

    private sealed class EjecutorTransaccionFalso :
        IEjecutorTransaccionSerializable
    {
        public Task<T> EjecutarAsync<T>(
            Func<CancellationToken, Task<T>> operacion,
            CancellationToken cancellationToken = default)
        {
            return operacion(cancellationToken);
        }
    }

    private sealed class TimeProviderFalso : TimeProvider
    {
        private readonly DateTimeOffset _fecha;

        public TimeProviderFalso(DateTimeOffset fecha)
        {
            _fecha = fecha;
        }

        public override DateTimeOffset GetUtcNow() => _fecha;
    }
}
