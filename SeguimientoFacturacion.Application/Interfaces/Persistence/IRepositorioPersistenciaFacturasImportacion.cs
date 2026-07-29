using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Define las operaciones necesarias para trasladar
/// las facturas válidas desde staging hacia las tablas
/// definitivas.
/// </summary>
public interface
    IRepositorioPersistenciaFacturasImportacion
{
    /// <summary>
    /// Obtiene los pacientes que ya existen para las
    /// identificaciones solicitadas.
    /// </summary>
    Task<IReadOnlyList<Paciente>>
        ListarPacientesExistentesAsync(
            IReadOnlyCollection<
                IdentificacionPacienteImportacionDto>
                identificaciones,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los identificadores de las facturas que
    /// ya existen en la tabla definitiva.
    /// </summary>
    Task<IReadOnlyList<string>>
        ListarIdentificadoresFacturasExistentesAsync(
            IReadOnlyCollection<string> identificadores,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega pacientes nuevos al contexto de persistencia.
    /// No confirma los cambios.
    /// </summary>
    Task AgregarPacientesAsync(
        IReadOnlyCollection<Paciente> pacientes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega facturas nuevas al contexto de persistencia.
    /// No confirma los cambios.
    /// </summary>
    Task AgregarFacturasAsync(
        IReadOnlyCollection<Factura> facturas,
        CancellationToken cancellationToken = default);
}