using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia de los movimientos financieros.
/// </summary>
internal sealed class MovimientoConfiguration :
    IEntityTypeConfiguration<Movimiento>
{
    private const int UsuarioAuditoriaLongitudMaxima = 100;

    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<Movimiento> builder)
    {
        builder.ToTable(
            "Movimientos",
            EsquemasBaseDatos.Facturacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Movimientos_Anio",
                    $"[Anio] BETWEEN " +
                    $"{Movimiento.AnioMinimo} AND " +
                    $"{Movimiento.AnioMaximo}");

                tableBuilder.HasCheckConstraint(
                    "CK_Movimientos_Valor",
                    "[Valor] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_Movimientos_NumeroNotaCredito",
                    "([TipoMovimientoId] = 1 " +
                    "AND NULLIF(LTRIM(RTRIM([NumeroNotaCredito])), '') " +
                    "IS NOT NULL) " +
                    "OR ([TipoMovimientoId] <> 1 " +
                    "AND [NumeroNotaCredito] IS NULL)");
            });

        builder.HasKey(movimiento => movimiento.Id);

        builder.Property(movimiento => movimiento.Id)
            .ValueGeneratedOnAdd();

        builder.Property(movimiento => movimiento.FacturaId)
            .HasMaxLength(Movimiento.FacturaIdLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(movimiento => movimiento.TipoMovimientoId)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(movimiento => movimiento.Anio)
            .IsRequired();

        builder.Property(movimiento => movimiento.Fecha)
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(movimiento => movimiento.Valor)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(movimiento =>
                movimiento.NumeroNotaCredito)
            .HasMaxLength(
                Movimiento.NumeroNotaCreditoLongitudMaxima)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(movimiento => movimiento.Observacion)
            .HasMaxLength(Movimiento.ObservacionLongitudMaxima)
            .IsUnicode();

        builder.Property(movimiento =>
                movimiento.FechaCreacionUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(movimiento => movimiento.CreadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(movimiento =>
                movimiento.FechaModificacionUtc)
            .HasPrecision(0);

        builder.Property(movimiento => movimiento.ModificadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false);

        builder.HasOne(movimiento =>
                movimiento.TipoMovimiento)
            .WithMany()
            .HasForeignKey(movimiento =>
                movimiento.TipoMovimientoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(
                movimiento => new
                {
                    movimiento.FacturaId,
                    movimiento.Anio,
                    movimiento.Fecha
                })
            .HasDatabaseName(
                "IX_Movimientos_FacturaId_Anio_Fecha");

        builder.HasIndex(movimiento =>
                movimiento.TipoMovimientoId)
            .HasDatabaseName(
                "IX_Movimientos_TipoMovimientoId");
    }
}