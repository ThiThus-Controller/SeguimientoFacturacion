using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Infrastructure.Persistence;

/// <summary>
/// Representa la sesión de trabajo de Entity Framework Core
/// para el sistema de seguimiento de facturación.
/// </summary>
public sealed class SeguimientoDbContext :
    DbContext,
    IUnidadTrabajo
{
    /// <summary>
    /// Inicializa una nueva instancia del contexto.
    /// </summary>
    public SeguimientoDbContext(
        DbContextOptions<SeguimientoDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Obtiene el conjunto de facturas.
    /// </summary>
    public DbSet<Factura> Facturas =>
        Set<Factura>();

    /// <summary>
    /// Obtiene las filas temporales de facturación.
    /// </summary>
    public DbSet<FacturaImportacionTemporal>
        FacturasTemporalesImportacion =>
            Set<FacturaImportacionTemporal>();

    /// <summary>
    /// Obtiene las notas almacenadas temporalmente.
    /// </summary>
    public DbSet<NotaFacturaImportacionTemporal>
        NotasFacturaTemporalesImportacion =>
            Set<NotaFacturaImportacionTemporal>();

    /// <summary>
    /// Obtiene las glosas almacenadas temporalmente.
    /// </summary>
    public DbSet<GlosaImportacionTemporal>
        GlosasTemporalesImportacion =>
            Set<GlosaImportacionTemporal>();

    /// <summary>
    /// Obtiene los pagos almacenados temporalmente.
    /// </summary>
    public DbSet<PagoImportacionTemporal>
        PagosTemporalesImportacion =>
            Set<PagoImportacionTemporal>();

    /// <summary>
    /// Obtiene las aplicaciones temporales de pagos.
    /// </summary>
    public DbSet<AplicacionPagoImportacionTemporal>
        AplicacionesPagoTemporalesImportacion =>
            Set<AplicacionPagoImportacionTemporal>();

    /// <summary>
    /// Obtiene el conjunto normalizado de pacientes.
    /// </summary>
    public DbSet<Paciente> Pacientes =>
        Set<Paciente>();

    /// <summary>
    /// Obtiene las notas crédito y débito.
    /// </summary>
    public DbSet<NotaFactura> NotasFactura =>
        Set<NotaFactura>();

    /// <summary>
    /// Obtiene las glosas asociadas a facturas.
    /// </summary>
    public DbSet<Glosa> Glosas =>
        Set<Glosa>();

    /// <summary>
    /// Obtiene los pagos recibidos.
    /// </summary>
    public DbSet<Pago> Pagos =>
        Set<Pago>();

    /// <summary>
    /// Obtiene las aplicaciones de pagos a facturas.
    /// </summary>
    public DbSet<AplicacionPago> AplicacionesPago =>
        Set<AplicacionPago>();

    /// <summary>
    /// Obtiene los lotes de importación masiva.
    /// </summary>
    public DbSet<LoteImportacion> LotesImportacion =>
        Set<LoteImportacion>();

    /// <summary>
    /// Obtiene las inconsistencias de importación.
    /// </summary>
    public DbSet<InconsistenciaImportacion>
        InconsistenciasImportacion =>
            Set<InconsistenciaImportacion>();

    /// <summary>
    /// Obtiene los registros históricos de auditoría.
    /// </summary>
    public DbSet<RegistroAuditoria>
        RegistrosAuditoria =>
            Set<RegistroAuditoria>();

    /// <summary>
    /// Obtiene los movimientos del modelo anterior.
    /// </summary>
    public DbSet<Movimiento> Movimientos =>
        Set<Movimiento>();

    /// <summary>
    /// Obtiene el catálogo de aseguradoras.
    /// </summary>
    public DbSet<Aseguradora> Aseguradoras =>
        Set<Aseguradora>();

    /// <summary>
    /// Obtiene el catálogo de atenciones.
    /// </summary>
    public DbSet<Atencion> Atenciones =>
        Set<Atencion>();

    /// <summary>
    /// Obtiene el catálogo de costos.
    /// </summary>
    public DbSet<Costo> Costos =>
        Set<Costo>();

    /// <summary>
    /// Obtiene el catálogo de estados.
    /// </summary>
    public DbSet<Estado> Estados =>
        Set<Estado>();

    /// <summary>
    /// Obtiene el catálogo de facturadores.
    /// </summary>
    public DbSet<Facturador> Facturadores =>
        Set<Facturador>();

    /// <summary>
    /// Obtiene el catálogo de tipos de documento.
    /// </summary>
    public DbSet<TipoDocumento> TiposDocumento =>
        Set<TipoDocumento>();

    /// <summary>
    /// Obtiene el catálogo anterior de movimientos.
    /// </summary>
    public DbSet<TipoMovimiento> TiposMovimiento =>
        Set<TipoMovimiento>();

    /// <inheritdoc />
    public async Task<int> GuardarCambiosAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var entidades =
                exception.Entries
                    .Select(
                        entrada =>
                            entrada.Metadata.ClrType.Name)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(
                        entidad => entidad,
                        StringComparer.Ordinal)
                    .ToArray();

            throw new ExcepcionConcurrenciaPersistencia(
                entidades,
                exception);
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SeguimientoDbContext).Assembly);
    }
}