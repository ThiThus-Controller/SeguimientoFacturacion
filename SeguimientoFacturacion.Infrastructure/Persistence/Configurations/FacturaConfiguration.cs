using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia de las facturas.
/// </summary>
internal sealed class FacturaConfiguration :
    IEntityTypeConfiguration<Factura>
{
    private const int UsuarioAuditoriaLongitudMaxima = 100;

    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<Factura> builder)
    {
        builder.ToTable(
            "Facturas",
            EsquemasBaseDatos.Facturacion);

        builder.HasKey(factura => factura.Id);

        builder.Property(factura => factura.Id)
            .HasMaxLength(Factura.IdLongitudMaxima)
            .IsUnicode(false)
            .ValueGeneratedNever();

        builder.Property(factura => factura.Prefijo)
            .HasMaxLength(Factura.PrefijoLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(factura => factura.Numero)
            .HasMaxLength(Factura.NumeroLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(factura => factura.FechaFactura)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(factura => factura.Valor)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(factura => factura.FechaRadicacion)
            .HasColumnType("date");

        builder.Property(factura => factura.NumeroDocumento)
            .HasMaxLength(Factura.NumeroDocumentoLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(factura => factura.NombreCompleto)
            .HasMaxLength(Factura.NombreCompletoLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(factura => factura.NumeroAdmision)
            .HasMaxLength(Factura.NumeroAdmisionLongitudMaxima)
            .IsUnicode(false);

        builder.Property(factura => factura.FechaAdmision)
            .HasColumnType("date");

        builder.Property(factura => factura.FechaCreacionUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(factura => factura.CreadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(factura => factura.FechaModificacionUtc)
            .HasPrecision(0);

        builder.Property(factura => factura.ModificadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false);

        builder.Property(factura => factura.VersionFila)
            .IsRowVersion();

        builder.Ignore(factura => factura.DiasHastaRadicacion);
        builder.Ignore(factura => factura.TotalNotasCredito);
        builder.Ignore(factura => factura.TotalAbonos);
        builder.Ignore(
            factura => factura.TotalGlosasODevoluciones);
        builder.Ignore(factura => factura.TotalConciliaciones);
        builder.Ignore(factura => factura.Saldo);

        builder.HasOne(factura => factura.Aseguradora)
            .WithMany()
            .HasForeignKey(factura => factura.AseguradoraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(factura => factura.TipoDocumento)
            .WithMany()
            .HasForeignKey(factura => factura.TipoDocumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Paciente>()
            .WithMany()
            .HasForeignKey(
                factura => new
                {
                    factura.TipoDocumentoId,
                    factura.NumeroDocumento
                })
            .HasPrincipalKey(
                paciente => new
                {
                    paciente.TipoDocumentoId,
                    paciente.NumeroDocumento
                })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "FK_Facturas_Pacientes_Identificacion");

        builder.HasOne(factura => factura.Atencion)
            .WithMany()
            .HasForeignKey(factura => factura.AtencionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(factura => factura.Costo)
            .WithMany()
            .HasForeignKey(factura => factura.CostoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(factura => factura.Estado)
            .WithMany()
            .HasForeignKey(factura => factura.EstadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(factura => factura.Facturador)
            .WithMany()
            .HasForeignKey(factura => factura.FacturadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(factura => factura.Movimientos)
            .WithOne(movimiento => movimiento.Factura)
            .HasForeignKey(movimiento => movimiento.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(factura => factura.Movimientos)
            .HasField("_movimientos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(factura => factura.FechaFactura)
            .HasDatabaseName("IX_Facturas_FechaFactura");

        builder.HasIndex(factura => factura.AseguradoraId)
            .HasDatabaseName("IX_Facturas_AseguradoraId");

        builder.HasIndex(factura => factura.EstadoId)
            .HasDatabaseName("IX_Facturas_EstadoId");

        builder.HasIndex(factura => factura.FacturadorId)
            .HasDatabaseName("IX_Facturas_FacturadorId");
    }
}
