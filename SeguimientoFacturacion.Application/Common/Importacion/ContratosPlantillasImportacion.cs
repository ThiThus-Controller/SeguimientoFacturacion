using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Common.Importacion;

/// <summary>
/// Contiene los contratos oficiales de las plantillas
/// modulares de importación.
/// </summary>
public static class ContratosPlantillasImportacion
{
    /// <summary>
    /// Número de la fila que contiene los encabezados.
    /// </summary>
    public const int FilaEncabezados = 1;

    /// <summary>
    /// Número de la primera fila destinada a datos.
    /// </summary>
    public const int PrimeraFilaDatos = 2;

    /// <summary>
    /// Obtiene el contrato de facturas.
    /// </summary>
    public static ContratoPlantillaImportacion Facturas
    {
        get;
    } = new(
        TipoImportacion.Facturas,
        "FACTURAS",
        [
            "FE",
            "PREFIJO",
            "FACTURA",
            "FECHA FACTURA",
            "ASEGURADORA",
            "VALOR",
            "FECHA DE RADICACION",
            "TIPO DTO",
            "NUMERO DTO",
            "NOMBRE COMPLETO",
            "ATENCION",
            "COSTO",
            "NO ADMISION",
            "FECHA ADMISION",
            "ESTADO DE DTO",
            "FACTURADOR"
        ]);

    /// <summary>
    /// Obtiene el contrato de notas crédito y débito.
    /// </summary>
    public static ContratoPlantillaImportacion
        NotasFactura
    {
        get;
    } = new(
        TipoImportacion.NotasFactura,
        "NOTAS",
        [
            "FE",
            "PREFIJO",
            "FACTURA",
            "ASEGURADORA",
            "TIPO NOTA",
            "FECHA NOTA",
            "NUMERO NOTA",
            "VALOR NOTA"
        ]);

    /// <summary>
    /// Obtiene el contrato de glosas.
    /// La fecha de respuesta pertenece a la estructura,
    /// pero su valor puede estar vacío.
    /// </summary>
    public static ContratoPlantillaImportacion Glosas
    {
        get;
    } = new(
        TipoImportacion.Glosas,
        "GLOSAS",
        [
            "FE",
            "PREFIJO",
            "FACTURA",
            "ASEGURADORA",
            "FECHA GLOSA",
            "VALOR GLOSA",
            "FECHA RTA GLOSA"
        ],
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["FECHA RESPUESTA GLOSA"] =
                "FECHA RTA GLOSA"
        });

    /// <summary>
    /// Obtiene el contrato de pagos y aplicaciones.
    /// </summary>
    public static ContratoPlantillaImportacion Pagos
    {
        get;
    } = new(
        TipoImportacion.Pagos,
        "PAGOS",
        [
            "FE",
            "PREFIJO",
            "FACTURA",
            "ASEGURADORA",
            "VALOR PAGADO",
            "VALOR CRUZADO",
            "RETENCION",
            "RETE ICA",
            "SALDO FAVOR",
            "SALDO CRUZADO PENDIENTE",
            "VR PAGADO",
            "VR CRUZADO",
            "FECHA DE PAGO",
            "RECIBO",
            "NOTAS"
        ],
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["SALDO RETENCION"] =
                "SALDO CRUZADO PENDIENTE"
        });

    private static readonly
        IReadOnlyList<ContratoPlantillaImportacion>
        ContratosModulares =
            Array.AsReadOnly(
                new[]
                {
                    Facturas,
                    NotasFactura,
                    Glosas,
                    Pagos
                });

    /// <summary>
    /// Obtiene todos los contratos modulares.
    /// </summary>
    public static IReadOnlyList<
        ContratoPlantillaImportacion> Todos =>
            ContratosModulares;

    /// <summary>
    /// Obtiene el contrato correspondiente al tipo indicado.
    /// </summary>
    public static ContratoPlantillaImportacion Obtener(
        TipoImportacion tipo)
    {
        return ContratosModulares
                   .SingleOrDefault(
                       contrato =>
                           contrato.Tipo == tipo)
               ??
               throw new ArgumentOutOfRangeException(
                   nameof(tipo),
                   tipo,
                   "El tipo indicado no tiene una plantilla " +
                   "modular registrada.");
    }

    /// <summary>
    /// Detecta el contrato correspondiente a un conjunto
    /// de encabezados.
    /// </summary>
    public static ContratoPlantillaImportacion? Detectar(
        IEnumerable<string?> encabezados)
    {
        ArgumentNullException.ThrowIfNull(encabezados);

        var encabezadosMaterializados =
            encabezados.ToArray();

        var coincidencias =
            ContratosModulares
                .Where(
                    contrato =>
                        contrato.CoincideCon(
                            encabezadosMaterializados))
                .ToArray();

        return coincidencias.Length switch
        {
            0 => null,
            1 => coincidencias[0],

            _ => throw new InvalidOperationException(
                "Los encabezados coinciden con más de un " +
                "contrato de importación.")
        };
    }
}