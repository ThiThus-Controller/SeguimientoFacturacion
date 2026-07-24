using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities;

/// <summary>
/// Representa una factura sometida al proceso de seguimiento,
/// radicación, pago, glosa y conciliación.
/// </summary>
public sealed class Factura : EntidadAuditableBase<string>
{
    /// <summary>
    /// Longitud máxima del identificador FE.
    /// </summary>
    public const int IdLongitudMaxima = 50;

    /// <summary>
    /// Longitud máxima del prefijo.
    /// </summary>
    public const int PrefijoLongitudMaxima = 50;

    /// <summary>
    /// Longitud máxima del número de factura.
    /// </summary>
    public const int NumeroLongitudMaxima = 50;

    /// <summary>
    /// Longitud máxima del número de documento.
    /// </summary>
    public const int NumeroDocumentoLongitudMaxima = 50;

    /// <summary>
    /// Longitud máxima del nombre del paciente.
    /// </summary>
    public const int NombreCompletoLongitudMaxima = 255;

    /// <summary>
    /// Longitud máxima del número de admisión.
    /// </summary>
    public const int NumeroAdmisionLongitudMaxima = 50;

    private readonly List<Movimiento> _movimientos = new();

    private Factura()
    {
    }

    /// <summary>
    /// Inicializa una nueva factura.
    /// </summary>
    public Factura(
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
        : base(ConstruirId(prefijo, numero))
    {
        Prefijo = ValidarTextoRequerido(
            prefijo,
            nameof(prefijo),
            PrefijoLongitudMaxima,
            convertirMayusculas: true);

        Numero = ValidarTextoRequerido(
            numero,
            nameof(numero),
            NumeroLongitudMaxima,
            convertirMayusculas: true);

        FechaFactura = ValidarFechaFactura(fechaFactura);

        AseguradoraId = ValidarCatalogoId(
            aseguradoraId,
            nameof(aseguradoraId));

        Valor = ValidarValor(valor);

        FechaRadicacion = ValidarFechaRadicacion(
            FechaFactura,
            fechaRadicacion);

        ActualizarPaciente(
            tipoDocumentoId,
            numeroDocumento,
            nombreCompleto);

        ActualizarDatosAtencion(
            atencionId,
            costoId,
            numeroAdmision,
            fechaAdmision);

        EstadoId = ValidarCatalogoId(
            estadoId,
            nameof(estadoId));

        FacturadorId = ValidarCatalogoId(
            facturadorId,
            nameof(facturadorId));
    }

    /// <summary>
    /// Obtiene el prefijo de la factura.
    /// </summary>
    public string Prefijo { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el número de la factura.
    /// </summary>
    public string Numero { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene la fecha de emisión de la factura.
    /// </summary>
    public DateOnly FechaFactura { get; private set; }

    /// <summary>
    /// Obtiene el código de la aseguradora.
    /// </summary>
    public int AseguradoraId { get; private set; }

    /// <summary>
    /// Obtiene el valor original de la factura.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Obtiene la fecha de radicación.
    /// Será nula cuando la factura no esté radicada.
    /// </summary>
    public DateOnly? FechaRadicacion { get; private set; }

    /// <summary>
    /// Obtiene el código del tipo de documento del paciente.
    /// </summary>
    public int TipoDocumentoId { get; private set; }

    /// <summary>
    /// Obtiene el número de documento del paciente.
    /// Se almacena como texto para conservar ceros y letras.
    /// </summary>
    public string NumeroDocumento { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el nombre completo del paciente.
    /// </summary>
    public string NombreCompleto { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el código del tipo de atención.
    /// </summary>
    public int AtencionId { get; private set; }

    /// <summary>
    /// Obtiene el código del centro o categoría de costo.
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
    /// Obtiene el código del estado de la factura.
    /// </summary>
    public int EstadoId { get; private set; }

    /// <summary>
    /// Obtiene el código del facturador.
    /// </summary>
    public int FacturadorId { get; private set; }

    /// <summary>
    /// Obtiene los días transcurridos entre la fecha de factura
    /// y la fecha de radicación.
    /// </summary>
    public int? DiasHastaRadicacion =>
        FechaRadicacion.HasValue
            ? FechaRadicacion.Value.DayNumber - FechaFactura.DayNumber
            : null;

    /// <summary>
    /// Obtiene los movimientos asociados a la factura.
    /// </summary>
    public IReadOnlyCollection<Movimiento> Movimientos => _movimientos;

    /// <summary>
    /// Obtiene el total de notas crédito.
    /// </summary>
    public decimal TotalNotasCredito =>
        CalcularTotalMovimiento(TipoMovimientoCodigo.NotaCredito);

    /// <summary>
    /// Obtiene el total de abonos.
    /// </summary>
    public decimal TotalAbonos =>
        CalcularTotalMovimiento(TipoMovimientoCodigo.Abono);

    /// <summary>
    /// Obtiene el total de glosas y devoluciones.
    /// </summary>
    public decimal TotalGlosasODevoluciones =>
        CalcularTotalMovimiento(TipoMovimientoCodigo.GlosaODevolucion);

    /// <summary>
    /// Obtiene el total de conciliaciones.
    /// </summary>
    public decimal TotalConciliaciones =>
        CalcularTotalMovimiento(TipoMovimientoCodigo.Conciliacion);

    /// <summary>
    /// Obtiene el saldo pendiente.
    /// Las notas crédito y los abonos disminuyen el saldo.
    /// </summary>
    public decimal Saldo =>
        Valor -
        TotalNotasCredito -
        TotalAbonos;

    // Propiedades de navegación

    /// <summary>
    /// Obtiene la aseguradora asociada.
    /// </summary>
    public Aseguradora? Aseguradora { get; private set; }

    /// <summary>
    /// Obtiene el tipo de documento asociado.
    /// </summary>
    public TipoDocumento? TipoDocumento { get; private set; }

    /// <summary>
    /// Obtiene el tipo de atención asociado.
    /// </summary>
    public Atencion? Atencion { get; private set; }

    /// <summary>
    /// Obtiene el centro o categoría de costo.
    /// </summary>
    public Costo? Costo { get; private set; }

    /// <summary>
    /// Obtiene el estado asociado.
    /// </summary>
    public Estado? Estado { get; private set; }

    /// <summary>
    /// Obtiene el facturador asociado.
    /// </summary>
    public Facturador? Facturador { get; private set; }

    /// <summary>
    /// Cambia la aseguradora responsable de la factura.
    /// </summary>
    public void CambiarAseguradora(int aseguradoraId)
    {
        AseguradoraId = ValidarCatalogoId(
            aseguradoraId,
            nameof(aseguradoraId));
    }

    /// <summary>
    /// Cambia el valor original de la factura.
    /// </summary>
    public void CambiarValor(decimal valor)
    {
        Valor = ValidarValor(valor);
    }

    /// <summary>
    /// Registra o cambia la fecha de radicación.
    /// </summary>
    public void RegistrarRadicacion(DateOnly fechaRadicacion)
    {
        FechaRadicacion = ValidarFechaRadicacion(
            FechaFactura,
            fechaRadicacion);
    }

    /// <summary>
    /// Retira la fecha de radicación.
    /// </summary>
    public void RetirarRadicacion()
    {
        FechaRadicacion = null;
    }

    /// <summary>
    /// Cambia el estado de la factura.
    /// </summary>
    public void CambiarEstado(int estadoId)
    {
        EstadoId = ValidarCatalogoId(
            estadoId,
            nameof(estadoId));
    }

    /// <summary>
    /// Cambia el facturador responsable.
    /// </summary>
    public void CambiarFacturador(int facturadorId)
    {
        FacturadorId = ValidarCatalogoId(
            facturadorId,
            nameof(facturadorId));
    }

    /// <summary>
    /// Actualiza la información de identificación del paciente.
    /// </summary>
    public void ActualizarPaciente(
        int tipoDocumentoId,
        string numeroDocumento,
        string nombreCompleto)
    {
        var tipoDocumentoValidado = ValidarCatalogoId(
            tipoDocumentoId,
            nameof(tipoDocumentoId));

        var numeroDocumentoValidado = ValidarTextoRequerido(
            numeroDocumento,
            nameof(numeroDocumento),
            NumeroDocumentoLongitudMaxima,
            convertirMayusculas: true);

        var nombreCompletoValidado = ValidarTextoRequerido(
            nombreCompleto,
            nameof(nombreCompleto),
            NombreCompletoLongitudMaxima);

        TipoDocumentoId = tipoDocumentoValidado;
        NumeroDocumento = numeroDocumentoValidado;
        NombreCompleto = nombreCompletoValidado;
    }

    /// <summary>
    /// Actualiza la información de atención y admisión.
    /// </summary>
    public void ActualizarDatosAtencion(
        int atencionId,
        int costoId,
        string? numeroAdmision,
        DateOnly? fechaAdmision)
    {
        var atencionIdValidado = ValidarCatalogoId(
            atencionId,
            nameof(atencionId));

        var costoIdValidado = ValidarCatalogoId(
            costoId,
            nameof(costoId));

        var numeroAdmisionValidado = ValidarTextoOpcional(
            numeroAdmision,
            nameof(numeroAdmision),
            NumeroAdmisionLongitudMaxima,
            convertirMayusculas: true);

        var fechaAdmisionValidada = ValidarFechaAdmision(
            FechaFactura,
            fechaAdmision);

        AtencionId = atencionIdValidado;
        CostoId = costoIdValidado;
        NumeroAdmision = numeroAdmisionValidado;
        FechaAdmision = fechaAdmisionValidada;
    }

    /// <summary>
    /// Agrega un movimiento a la factura.
    /// </summary>
    /// <param name="movimiento">Movimiento que será agregado.</param>
    public void AgregarMovimiento(Movimiento movimiento)
    {
        ArgumentNullException.ThrowIfNull(movimiento);

        if (!string.Equals(
                movimiento.FacturaId,
                Id,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "El movimiento no pertenece a esta factura.");
        }

        if (_movimientos.Contains(movimiento))
        {
            throw new InvalidOperationException(
                "El movimiento ya se encuentra asociado a la factura.");
        }

        if (movimiento.Id > 0 &&
            _movimientos.Any(item => item.Id == movimiento.Id))
        {
            throw new InvalidOperationException(
                "Ya existe un movimiento con el mismo identificador.");
        }

        _movimientos.Add(movimiento);
    }

    /// <summary>
    /// Retira un movimiento de la factura.
    /// </summary>
    /// <param name="movimiento">Movimiento que será retirado.</param>
    /// <returns>
    /// Verdadero cuando el movimiento fue encontrado y retirado.
    /// </returns>
    public bool RetirarMovimiento(Movimiento movimiento)
    {
        ArgumentNullException.ThrowIfNull(movimiento);

        return _movimientos.Remove(movimiento);
    }

    private decimal CalcularTotalMovimiento(
        TipoMovimientoCodigo tipoMovimiento)
    {
        return _movimientos
            .Where(movimiento =>
                movimiento.TipoMovimientoId == tipoMovimiento)
            .Sum(movimiento => movimiento.Valor);
    }

    private static string ConstruirId(
        string prefijo,
        string numero)
    {
        var prefijoValidado = ValidarTextoRequerido(
            prefijo,
            nameof(prefijo),
            PrefijoLongitudMaxima,
            convertirMayusculas: true);

        var numeroValidado = ValidarTextoRequerido(
            numero,
            nameof(numero),
            NumeroLongitudMaxima,
            convertirMayusculas: true);

        var id = $"{prefijoValidado}{numeroValidado}";

        if (id.Length > IdLongitudMaxima)
        {
            throw new ArgumentException(
                $"La combinación del prefijo y número no puede superar los {IdLongitudMaxima} caracteres.");
        }

        return id;
    }

    private static int ValidarCatalogoId(
        int id,
        string nombreParametro)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                id,
                "El código del catálogo debe ser mayor que cero.");
        }

        return id;
    }

    private static DateOnly ValidarFechaFactura(
        DateOnly fechaFactura)
    {
        if (fechaFactura == default)
        {
            throw new ArgumentException(
                "La fecha de la factura es obligatoria.",
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
                "La fecha de radicación no puede ser anterior a la fecha de la factura.");
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
                "La fecha de admisión no puede ser posterior a la fecha de la factura.");
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
                "El valor de la factura debe ser mayor que cero.");
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
            valorNormalizado = valorNormalizado.ToUpperInvariant();
        }

        if (valorNormalizado.Length > longitudMaxima)
        {
            throw new ArgumentException(
                $"El valor no puede superar los {longitudMaxima} caracteres.",
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
            valorNormalizado = valorNormalizado.ToUpperInvariant();
        }

        if (valorNormalizado.Length > longitudMaxima)
        {
            throw new ArgumentException(
                $"El valor no puede superar los {longitudMaxima} caracteres.",
                nombreParametro);
        }

        return valorNormalizado;
    }
}