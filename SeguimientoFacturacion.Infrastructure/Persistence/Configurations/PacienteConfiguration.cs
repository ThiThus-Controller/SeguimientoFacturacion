using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia de los pacientes.
/// </summary>
internal sealed class PacienteConfiguration :
    IEntityTypeConfiguration<Paciente>
{
    private const int UsuarioAuditoriaLongitudMaxima = 100;

    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable(
            "Pacientes",
            EsquemasBaseDatos.Facturacion);

        builder.HasKey(paciente => paciente.Id);

        builder.Property(paciente => paciente.Id)
            .ValueGeneratedNever();

        builder.Property(paciente => paciente.TipoDocumentoId)
            .IsRequired();

        builder.Property(paciente => paciente.NumeroDocumento)
            .HasMaxLength(
                Paciente.NumeroDocumentoLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(paciente => paciente.NombreCompleto)
            .HasMaxLength(
                Paciente.NombreCompletoLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(paciente => paciente.FechaCreacionUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(paciente => paciente.CreadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(paciente => paciente.FechaModificacionUtc)
            .HasPrecision(0);

        builder.Property(paciente => paciente.ModificadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false);

        builder.HasOne(paciente => paciente.TipoDocumento)
            .WithMany()
            .HasForeignKey(paciente => paciente.TipoDocumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(
                paciente => new
                {
                    paciente.TipoDocumentoId,
                    paciente.NumeroDocumento
                })
            .HasName(
                "AK_Pacientes_TipoDocumento_NumeroDocumento");

        builder.HasIndex(paciente => paciente.NombreCompleto)
            .HasDatabaseName(
                "IX_Pacientes_NombreCompleto");
    }
}