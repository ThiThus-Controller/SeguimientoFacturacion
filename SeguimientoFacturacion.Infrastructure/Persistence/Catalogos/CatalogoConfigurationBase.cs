using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Define la configuración común para los catálogos
/// identificados mediante un código numérico.
/// </summary>
/// <typeparam name="TEntidad">
/// Tipo de entidad de catálogo.
/// </typeparam>
internal abstract class CatalogoConfigurationBase<TEntidad> :
    IEntityTypeConfiguration<TEntidad>
    where TEntidad : CatalogoBase
{
    /// <summary>
    /// Obtiene el nombre de la tabla del catálogo.
    /// </summary>
    protected abstract string NombreTabla { get; }

    /// <inheritdoc />
    public virtual void Configure(
        EntityTypeBuilder<TEntidad> builder)
    {
        builder.ToTable(
            NombreTabla,
            EsquemasBaseDatos.Facturacion);

        builder.HasKey(entidad => entidad.Id);

        builder.Property(entidad => entidad.Id)
            .ValueGeneratedNever();

        builder.Property(entidad => entidad.Descripcion)
            .HasMaxLength(CatalogoBase.DescripcionLongitudMaxima)
            .IsUnicode()
            .IsRequired();
    }
}