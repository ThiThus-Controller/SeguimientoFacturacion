using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia de los registros
/// inmutables de auditoría.
/// </summary>
internal sealed class RegistroAuditoriaConfiguration :
    IEntityTypeConfiguration<RegistroAuditoria>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable(
            "RegistrosAuditoria",
            EsquemasBaseDatos.Auditoria);

        builder.HasKey(registro => registro.Id);

        builder.Property(registro => registro.Id)
            .ValueGeneratedNever();

        builder.Property(registro => registro.TipoOperacion)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(registro => registro.NombreEntidad)
            .HasMaxLength(
                RegistroAuditoria.NombreEntidadLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(registro => registro.EntidadId)
            .HasMaxLength(
                RegistroAuditoria.EntidadIdLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(registro => registro.Usuario)
            .HasMaxLength(
                RegistroAuditoria.UsuarioLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(registro => registro.FechaUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(
                registro => registro.DatosAnterioresJson)
            .HasMaxLength(
                RegistroAuditoria.DatosJsonLongitudMaxima)
            .IsUnicode();

        builder.Property(
                registro => registro.DatosNuevosJson)
            .HasMaxLength(
                RegistroAuditoria.DatosJsonLongitudMaxima)
            .IsUnicode();

        builder.Property(registro => registro.Motivo)
            .HasMaxLength(
                RegistroAuditoria.MotivoLongitudMaxima)
            .IsUnicode();

        builder.Property(registro => registro.CorrelacionId);

        builder.HasIndex(
                registro => new
                {
                    registro.NombreEntidad,
                    registro.EntidadId,
                    registro.FechaUtc
                })
            .HasDatabaseName(
                "IX_RegistrosAuditoria_Entidad_FechaUtc");

        builder.HasIndex(registro => registro.CorrelacionId)
            .HasDatabaseName(
                "IX_RegistrosAuditoria_CorrelacionId")
            .HasFilter("[CorrelacionId] IS NOT NULL");

        builder.HasIndex(
                registro => new
                {
                    registro.Usuario,
                    registro.FechaUtc
                })
            .HasDatabaseName(
                "IX_RegistrosAuditoria_Usuario_FechaUtc");
    }
}