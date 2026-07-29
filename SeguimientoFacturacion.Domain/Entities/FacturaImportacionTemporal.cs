using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa una fila válida de facturación almacenada
/// temporalmente antes de confirmar su importación.
/// </summary>
public sealed class FacturaImportacionTemporal :
    EntidadBase<Guid>
{
    /// <summary>
    /// Longitud máxima del nombre de una hoja de Excel.
    /// </summary>
    public const int HojaOrigenLongitudMaxima = 31;

    private FacturaImportacionTemporal()
    {
    }

    /// <summary>
    /// Inicializa una fila temporal de facturación.
    /// </summary>
    public FacturaImportacionTemporal(
        Guid loteImportacionId,
        string hojaOrigen,
        int filaOrigen,
        string identificadorFe,
        string prefijo,
        string numero,
        DateOnly fechaFactura,
        int aseguradoraId,
        decimal valor,
        DateOnly? fechaRadicacion,
        int tipoDocumentoId,
        string numeroDocumento,
        string nombreCompleto,
        int atencionId,
        int costoId,
        string? numeroAdmision,
        DateOnly? fechaAdmision,
        int estadoId,
        int facturadorId)
        : base(Guid.NewGuid())
    {
        LoteImportacionId =
            ValidarLoteImportacionId(
                loteImportacionId);

        HojaOrigen = ValidarTextoRequerido(
            hojaOrigen,
            nameof(hojaOrigen),
            HojaOrigenLongitudMaxima);

        FilaOrigen = ValidarFilaOrigen(
            filaOrigen);

        IdentificadorFe = ValidarTextoRequerido(
            identificadorFe,
            nameof(identificadorFe),
            Factura.IdLongitudMaxima,
            convertirMayusculas: true);

        Prefijo = ValidarTextoRequerido(
            prefijo,
            nameof(prefijo),
            Factura.PrefijoLongitudMaxima,
            convertirMayusculas: true);

        Numero = ValidarTextoRequerido(
            numero,
            nameof(numero),
            Factura.NumeroLongitudMaxima,
            convertirMayusculas: true);

        FechaFactura = ValidarFechaFactura(
            fechaFactura);

        AseguradoraId = ValidarCatalogoId(
            aseguradoraId,
            nameof(aseguradoraId));

        Valor = ValidarValor(valor);

        FechaRadicacion = ValidarFechaRadicacion(
            FechaFactura,
            fechaRadicacion);

        TipoDocumentoId = ValidarCatalogoId(
            tipoDocumentoId,
            nameof(tipoDocumentoId));

        NumeroDocumento = ValidarTextoRequerido(
            numeroDocumento,
            nameof(numeroDocumento),
            Paciente.NumeroDocumentoLongitudMaxima,
            convertirMayusculas: true);

        NombreCompleto = ValidarTextoRequerido(
            nombreCompleto,
            nameof(nombreCompleto),
            Paciente.NombreCompletoLongitudMaxima);

        AtencionId = ValidarCatalogoId(
            atencionId,
            nameof(atencionId));

        CostoId = ValidarCatalogoId(
            costoId,
            nameof(costoId));

        NumeroAdmision = ValidarTextoOpcional(
            numeroAdmision,
            nameof(numeroAdmision),
            Factura.NumeroAdmisionLongitudMaxima,
            convertirMayusculas: true);

        FechaAdmision = ValidarFechaAdmision(
            FechaFactura,
            fechaAdmision);

        EstadoId = ValidarCatalogoId(
            estadoId,
            nameof(estadoId));

        FacturadorId = ValidarCatalogoId(
            facturadorId,
            nameof(facturadorId));
    }

    /// <summary>
    /// Obtiene el lote al que pertenece la fila.
    /// </summary>
    public Guid LoteImportacionId { get; private set; }

    /// <summary>
    /// Obtiene el nombre de la hoja de origen.
    /// </summary>
    public string HojaOrigen { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el número de fila original del Excel.
    /// </summary>
    public int FilaOrigen { get; private set; }

    /// <summary>
    /// Obtiene el identificador FE presentado.
    /// </summary>
    public string IdentificadorFe { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el prefijo de la factura.
    /// </summary>
    public string Prefijo { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el número de factura.
    /// </summary>
    public string Numero { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene la fecha de emisión.
    /// </summary>
    public DateOnly FechaFactura { get; private set; }

    /// <summary>
    /// Obtiene la aseguradora identificada.
    /// </summary>
    public int AseguradoraId { get; private set; }

    /// <summary>
    /// Obtiene el valor original de la factura.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Obtiene la fecha de radicación.
    /// </summary>
    public DateOnly? FechaRadicacion { get; private set; }

    /// <summary>
    /// Obtiene el tipo de documento del paciente.
    /// </summary>
    public int TipoDocumentoId { get; private set; }

    /// <summary>
    /// Obtiene el número de documento del paciente.
    /// </summary>
    public string NumeroDocumento { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el nombre completo del paciente.
    /// </summary>
    public string NombreCompleto { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el tipo de atención.
    /// </summary>
    public int AtencionId { get; private set; }

    /// <summary>
    /// Obtiene el centro o categoría de costo.
    /// </summary>
    public int CostoId { get; private set; }

    /// <summary>
    /// Obtiene el número de admisión.
    /// </summary>
    public string? NumeroAdmision { get; private set; }

    /// <summary>
    /// Obtiene la fecha de admisión.
    /// </summary>
    public DateOnly? FechaAdmision { get; private set; }

    /// <summary>
    /// Obtiene el estado identificado.
    /// </summary>
    public int EstadoId { get; private set; }

    /// <summary>
    /// Obtiene el facturador identificado.
    /// </summary>
    public int FacturadorId { get; private set; }

    /// <summary>
    /// Obtiene el lote de importación asociado.
    /// </summary>
    public LoteImportacion? LoteImportacion
    {
        get;
        private set;
    }

    private static Guid ValidarLoteImportacionId(
        Guid loteImportacionId)
    {
        if (loteImportacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del lote es obligatorio.",
                nameof(loteImportacionId));
        }

        return loteImportacionId;
    }

    private static int ValidarFilaOrigen(int filaOrigen)
    {
        if (filaOrigen <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(filaOrigen),
                filaOrigen,
                "La fila de origen debe ser mayor que cero.");
        }

        return filaOrigen;
    }

    private static int ValidarCatalogoId(
        int identificador,
        string nombreParametro)
    {
        if (identificador <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                identificador,
                "El identificador del catálogo debe ser " +
                "mayor que cero.");
        }

        return identificador;
    }

    private static DateOnly ValidarFechaFactura(
        DateOnly fechaFactura)
    {
        if (fechaFactura == default)
        {
            throw new ArgumentException(
                "La fecha de factura es obligatoria.",
                nameof(fechaFactura));
        }

        return fechaFactura;
    }

    private static DateOnly? ValidarFechaRadicacion(
        DateOnly fechaFactura,
        DateOnly? fechaRadicacion)
    {
        if (!fechaRadicacion.HasValue)
        {
            return null;
        }

        if (fechaRadicacion.Value < fechaFactura)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaRadicacion),
                fechaRadicacion,
                "La fecha de radicación no puede ser " +
                "anterior a la fecha de factura.");
        }

        return fechaRadicacion;
    }

    private static DateOnly? ValidarFechaAdmision(
        DateOnly fechaFactura,
        DateOnly? fechaAdmision)
    {
        if (!fechaAdmision.HasValue)
        {
            return null;
        }

        if (fechaAdmision.Value > fechaFactura)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaAdmision),
                fechaAdmision,
                "La fecha de admisión no puede ser " +
                "posterior a la fecha de factura.");
        }

        return fechaAdmision;
    }

    private static decimal ValidarValor(decimal valor)
    {
        if (valor <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valor),
                valor,
                "El valor de la factura debe ser " +
                "mayor que cero.");
        }

        return valor;
    }

    private static string ValidarTextoRequerido(
        string valor,
        string nombreParametro,
        int longitudMaxima,
        bool convertirMayusculas = false)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El valor es obligatorio.",
                nombreParametro);
        }

        var valorNormalizado = valor.Trim();

        if (convertirMayusculas)
        {
            valorNormalizado =
                valorNormalizado.ToUpperInvariant();
        }

        if (valorNormalizado.Length > longitudMaxima)
        {
            throw new ArgumentException(
                $"El valor no puede superar los " +
                $"{longitudMaxima} caracteres.",
                nombreParametro);
        }

        return valorNormalizado;
    }

    private static string? ValidarTextoOpcional(
        string? valor,
        string nombreParametro,
        int longitudMaxima,
        bool convertirMayusculas = false)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var valorNormalizado = valor.Trim();

        if (convertirMayusculas)
        {
            valorNormalizado =
                valorNormalizado.ToUpperInvariant();
        }

        if (valorNormalizado.Length > longitudMaxima)
        {
            throw new ArgumentException(
                $"El valor no puede superar los " +
                $"{longitudMaxima} caracteres.",
                nombreParametro);
        }

        return valorNormalizado;
    }
}