using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa una glosa almacenada temporalmente antes
/// de confirmar su importación definitiva.
/// </summary>
public sealed class GlosaImportacionTemporal :
    EntidadBase<Guid>
{
    /// <summary>
    /// Longitud máxima permitida para el nombre de una
    /// hoja de Excel.
    /// </summary>
    public const int HojaOrigenLongitudMaxima = 31;

    private GlosaImportacionTemporal()
    {
    }

    /// <summary>
    /// Inicializa una glosa temporal de importación.
    /// </summary>
    public GlosaImportacionTemporal(
        Guid loteImportacionId,
        string hojaOrigen,
        int filaOrigen,
        string identificadorFe,
        string prefijo,
        string numeroFactura,
        int aseguradoraId,
        DateOnly fechaGlosa,
        decimal valorGlosa,
        DateOnly? fechaRespuesta,
        EstadoGlosa? estado = null,
        decimal valorAceptado = decimal.Zero)
        : base(Guid.NewGuid())
    {
        LoteImportacionId =
            ValidarLoteImportacionId(
                loteImportacionId);

        HojaOrigen =
            ValidarTextoRequerido(
                hojaOrigen,
                nameof(hojaOrigen),
                HojaOrigenLongitudMaxima);

        FilaOrigen =
            ValidarFilaOrigen(filaOrigen);

        IdentificadorFe =
            ValidarTextoRequerido(
                identificadorFe,
                nameof(identificadorFe),
                Factura.IdLongitudMaxima,
                convertirMayusculas: true);

        Prefijo =
            ValidarTextoRequerido(
                prefijo,
                nameof(prefijo),
                Factura.PrefijoLongitudMaxima,
                convertirMayusculas: true);

        NumeroFactura =
            ValidarTextoRequerido(
                numeroFactura,
                nameof(numeroFactura),
                Factura.NumeroLongitudMaxima,
                convertirMayusculas: true);

        ValidarCorrespondenciaFactura(
            IdentificadorFe,
            Prefijo,
            NumeroFactura);

        AseguradoraId =
            ValidarCatalogoId(
                aseguradoraId,
                nameof(aseguradoraId));

        FechaGlosa =
            ValidarFechaGlosa(fechaGlosa);

        ValorGlosa =
            ValidarValorGlosa(valorGlosa);

        FechaRespuesta =
            ValidarFechaRespuesta(
                FechaGlosa,
                fechaRespuesta);

        Estado = NormalizarEstado(
            estado ??
            (FechaRespuesta.HasValue
                ? EstadoGlosa.Respondida
                : EstadoGlosa.Abierta),
            valorAceptado,
            ValorGlosa);

        ValorAceptado =
            ValidarResolucion(
                Estado,
                FechaRespuesta,
                valorAceptado,
                ValorGlosa);
    }

    /// <summary>
    /// Obtiene el lote de importación propietario.
    /// </summary>
    public Guid LoteImportacionId { get; private set; }

    /// <summary>
    /// Obtiene el nombre de la hoja de origen.
    /// </summary>
    public string HojaOrigen { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el número de fila original del archivo.
    /// </summary>
    public int FilaOrigen { get; private set; }

    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public string IdentificadorFe { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el prefijo de la factura.
    /// </summary>
    public string Prefijo { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el número de la factura.
    /// </summary>
    public string NumeroFactura { get; private set; } =
        string.Empty;

    /// <summary>
    /// Obtiene el identificador de la aseguradora
    /// validada.
    /// </summary>
    public int AseguradoraId { get; private set; }

    /// <summary>
    /// Obtiene la fecha de recepción de la glosa.
    /// </summary>
    public DateOnly FechaGlosa { get; private set; }

    /// <summary>
    /// Obtiene el valor total glosado.
    /// </summary>
    public decimal ValorGlosa { get; private set; }

    /// <summary>
    /// Obtiene la fecha de respuesta, cuando fue
    /// informada en el archivo.
    /// </summary>
    public DateOnly? FechaRespuesta { get; private set; }

    /// <summary>
    /// Obtiene el estado de gestión informado en el archivo.
    /// </summary>
    public EstadoGlosa Estado { get; private set; }

    /// <summary>
    /// Obtiene el valor aceptado por la institución.
    /// </summary>
    public decimal ValorAceptado { get; private set; }

    /// <summary>
    /// Indica si la glosa contiene una fecha de respuesta.
    /// </summary>
    public bool TieneRespuesta =>
        FechaRespuesta.HasValue;

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

    private static int ValidarFilaOrigen(
        int filaOrigen)
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

    private static DateOnly ValidarFechaGlosa(
        DateOnly fechaGlosa)
    {
        if (fechaGlosa == default)
        {
            throw new ArgumentException(
                "La fecha de la glosa es obligatoria.",
                nameof(fechaGlosa));
        }

        return fechaGlosa;
    }

    private static decimal ValidarValorGlosa(
        decimal valorGlosa)
    {
        if (valorGlosa <= decimal.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorGlosa),
                valorGlosa,
                "El valor de la glosa debe ser mayor " +
                "que cero.");
        }

        return valorGlosa;
    }

    private static DateOnly? ValidarFechaRespuesta(
        DateOnly fechaGlosa,
        DateOnly? fechaRespuesta)
    {
        if (!fechaRespuesta.HasValue)
        {
            return null;
        }

        if (fechaRespuesta.Value == default)
        {
            throw new ArgumentException(
                "La fecha de respuesta no es válida.",
                nameof(fechaRespuesta));
        }

        if (fechaRespuesta.Value < fechaGlosa)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaRespuesta),
                fechaRespuesta,
                "La fecha de respuesta no puede ser " +
                "anterior a la fecha de la glosa.");
        }

        return fechaRespuesta;
    }

    private static decimal ValidarResolucion(
        EstadoGlosa estado,
        DateOnly? fechaRespuesta,
        decimal valorAceptado,
        decimal valorGlosa)
    {
        if (!Enum.IsDefined(estado))
        {
            throw new ArgumentOutOfRangeException(
                nameof(estado),
                estado,
                "El estado de la glosa no es válido.");
        }

        if (estado == EstadoGlosa.Anulada)
        {
            throw new ArgumentException(
                "El estado anulada es exclusivo de la gestión " +
                "manual y no puede importarse.",
                nameof(estado));
        }

        if (valorAceptado < decimal.Zero ||
            valorAceptado > valorGlosa)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valorAceptado),
                valorAceptado,
                "El valor aceptado debe estar entre cero " +
                "y el valor de la glosa.");
        }

        if (estado == EstadoGlosa.Abierta)
        {
            if (fechaRespuesta.HasValue)
            {
                throw new ArgumentException(
                    "Una glosa abierta no puede tener fecha " +
                    "de respuesta.",
                    nameof(fechaRespuesta));
            }

            if (valorAceptado != decimal.Zero)
            {
                throw new ArgumentException(
                    "Una glosa abierta no puede tener valor " +
                    "aceptado.",
                    nameof(valorAceptado));
            }

            return decimal.Zero;
        }

        if (!fechaRespuesta.HasValue)
        {
            throw new ArgumentException(
                "El estado informado requiere fecha de " +
                "respuesta.",
                nameof(fechaRespuesta));
        }

        if (estado == EstadoGlosa.Aceptada &&
            valorAceptado != valorGlosa)
        {
            throw new ArgumentException(
                "Una glosa aceptada de forma definitiva debe " +
                "tener aceptado todo el valor glosado.",
                nameof(valorAceptado));
        }

        if (estado == EstadoGlosa.EnNegociacion &&
            (valorAceptado <= decimal.Zero ||
             valorAceptado >= valorGlosa))
        {
            throw new ArgumentException(
                "Una glosa en negociación debe tener un valor " +
                "aceptado mayor que cero y menor al valor glosado.",
                nameof(valorAceptado));
        }

        if ((estado is
                EstadoGlosa.Respondida or
                EstadoGlosa.Levantada) &&
            valorAceptado != decimal.Zero)
        {
            throw new ArgumentException(
                "Una glosa respondida o levantada no puede " +
                "tener valor aceptado.",
                nameof(valorAceptado));
        }

        return valorAceptado;
    }

    private static EstadoGlosa NormalizarEstado(
        EstadoGlosa estado,
        decimal valorAceptado,
        decimal valorGlosa)
    {
        return estado == EstadoGlosa.Aceptada &&
            valorAceptado > decimal.Zero &&
            valorAceptado < valorGlosa
                ? EstadoGlosa.EnNegociacion
                : estado;
    }

    private static void ValidarCorrespondenciaFactura(
        string identificadorFe,
        string prefijo,
        string numeroFactura)
    {
        var identificadorEsperado =
            $"{prefijo}{numeroFactura}";

        if (!string.Equals(
                identificadorFe,
                identificadorEsperado,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "El identificador FE debe coincidir con " +
                "la combinación del prefijo y el número " +
                "de factura.",
                nameof(identificadorFe));
        }
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

        var valorNormalizado =
            valor.Trim();

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
