using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene las notas crédito y débito preparadas
/// desde una plantilla modular validada.
/// </summary>
public sealed class
    ResultadoPreparacionNotasFacturaDto
{
    /// <summary>
    /// Obtiene el nombre del archivo procesado.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene las notas preparadas.
    /// </summary>
    public IReadOnlyCollection<
        NotaFacturaPreparadaImportacionDto>
        Notas
    {
        get;
        init;
    } = Array.Empty<
        NotaFacturaPreparadaImportacionDto>();

    /// <summary>
    /// Obtiene la cantidad total de notas.
    /// </summary>
    public int TotalNotas =>
        Notas.Count;

    /// <summary>
    /// Obtiene la cantidad de notas crédito.
    /// </summary>
    public int TotalNotasCredito =>
        Notas.Count(
            nota =>
                nota.Tipo ==
                TipoNotaFactura.Credito);

    /// <summary>
    /// Obtiene la cantidad de notas débito.
    /// </summary>
    public int TotalNotasDebito =>
        Notas.Count(
            nota =>
                nota.Tipo ==
                TipoNotaFactura.Debito);

    /// <summary>
    /// Obtiene el valor total de notas crédito.
    /// </summary>
    public decimal ValorTotalCredito =>
        Notas
            .Where(nota =>
                nota.Tipo ==
                TipoNotaFactura.Credito)
            .Sum(nota =>
                nota.ValorNota);

    /// <summary>
    /// Obtiene el valor total de notas débito.
    /// </summary>
    public decimal ValorTotalDebito =>
        Notas
            .Where(nota =>
                nota.Tipo ==
                TipoNotaFactura.Debito)
            .Sum(nota =>
                nota.ValorNota);

    /// <summary>
    /// Obtiene el impacto financiero neto esperado.
    /// Un resultado negativo disminuye el saldo y uno
    /// positivo lo incrementa.
    /// </summary>
    public decimal ImpactoNetoSaldo =>
        ValorTotalDebito -
        ValorTotalCredito;
}