namespace SeguimientoFacturacion.Application.Common.Models;

/// <summary>
/// Representa el resultado paginado de una consulta.
/// </summary>
/// <typeparam name="T">
/// Tipo de elemento incluido en el resultado.
/// </typeparam>
public sealed class ResultadoPaginado<T>
{
    /// <summary>
    /// Inicializa un nuevo resultado paginado.
    /// </summary>
    public ResultadoPaginado(
        IReadOnlyCollection<T> elementos,
        int totalRegistros,
        int pagina,
        int tamanoPagina)
    {
        ArgumentNullException.ThrowIfNull(elementos);

        if (totalRegistros < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalRegistros),
                totalRegistros,
                "El total de registros no puede ser negativo.");
        }

        if (pagina <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pagina),
                pagina,
                "La página debe ser mayor que cero.");
        }

        if (tamanoPagina <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tamanoPagina),
                tamanoPagina,
                "El tamaño de página debe ser mayor que cero.");
        }

        Elementos = elementos;
        TotalRegistros = totalRegistros;
        Pagina = pagina;
        TamanoPagina = tamanoPagina;
    }

    /// <summary>
    /// Obtiene los elementos de la página actual.
    /// </summary>
    public IReadOnlyCollection<T> Elementos { get; }

    /// <summary>
    /// Obtiene el total de registros de la consulta.
    /// </summary>
    public int TotalRegistros { get; }

    /// <summary>
    /// Obtiene el número de la página actual.
    /// </summary>
    public int Pagina { get; }

    /// <summary>
    /// Obtiene el tamaño máximo de la página.
    /// </summary>
    public int TamanoPagina { get; }

    /// <summary>
    /// Obtiene el total de páginas.
    /// </summary>
    public int TotalPaginas =>
        TotalRegistros == 0
            ? 0
            : (int)Math.Ceiling(
                TotalRegistros / (double)TamanoPagina);

    /// <summary>
    /// Indica si existe una página anterior.
    /// </summary>
    public bool TienePaginaAnterior => Pagina > 1;

    /// <summary>
    /// Indica si existe una página siguiente.
    /// </summary>
    public bool TienePaginaSiguiente => Pagina < TotalPaginas;
}