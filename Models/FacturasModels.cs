using Microsoft.AspNetCore.Components.Forms;
using System.Xml.Serialization;
using eCertify.Utils;
using System.Text.Json.Serialization;


namespace eCertify.Models
{
    [XmlRoot("ECF")]
    public class FacturasModels
    {
        public Encabezado? Encabezado { get; set; }

        [JsonConverter(typeof(AplanarListaConverterFactory))]
        public List<Item>? DetallesItems { get; set; }
        public List<Subtotal>? Subtotales { get; set; }
        public List<DescuentoORecargo>? DescuentosORecargos { get; set; }
        public List<Pagina>? Paginacion { get; set; }
        public InformacionReferencia? InformacionReferencia { get; set; }
        public string? FechaHoraFirma { get; set; }
        public string? any_element { get; set; }
    }

    // Encabezado y secciones anidadas (omitido aquí para evitar duplicación del archivo anterior)

    public class Encabezado
    {
        public string? Version { get; set; }
        public IdDoc? IdDoc { get; set; }
        public Emisor? Emisor { get; set; }
        public Comprador? Comprador { get; set; }
        public InformacionesAdicionales? InformacionesAdicionales { get; set; }
        public Transporte? Transporte { get; set; }
        public Totales? Totales { get; set; }
        public OtraMoneda? OtraMoneda { get; set; }
    }
    
    public class IdDoc
    {
        public string? TipoeCF { get; set; }
        public string? eNCF { get; set; }
        public string? IndicadorNotaCredito { get; set; }
        public string? FechaVencimientoSecuencia { get; set; }
        public string? IndicadorEnvioDiferido { get; set; }
        public string? IndicadorMontoGravado { get; set; }
        public string? IndicadorServicioTodoIncluido { get; set; }
        public string? TipoIngresos { get; set; }
        public string? TipoPago { get; set; }
        public string? FechaLimitePago { get; set; }
        public string? TerminoPago { get; set; }

        [JsonConverter(typeof(AplanarListaConverterFactory))]
        public List<FormaDePago>? TablaFormasPago { get; set; }
        public string? TipoCuentaPago { get; set; }
        public string? NumeroCuentaPago { get; set; }
        public string? BancoPago { get; set; }
        public string? FechaDesde { get; set; }
        public string? FechaHasta { get; set; }
        public string? TotalPaginas { get; set; }
    }

    public class FormaDePago
    {
        public string? FormaPago { get; set; }
        public string? MontoPago { get; set; }
    }

    public class Emisor
    {
        public string? RNCEmisor { get; set; }
        public string? RazonSocialEmisor { get; set; }
        public string? NombreComercial { get; set; }
        public string? Sucursal { get; set; }
        public string? DireccionEmisor { get; set; }
        public string? Municipio { get; set; }
        public string? Provincia { get; set; }
        public List<string>? TablaTelefonoEmisor { get; set; }
        public string? CorreoEmisor { get; set; }
        public string? WebSite { get; set; }
        public string? ActividadEconomica { get; set; }
        public string? CodigoVendedor { get; set; }
        public string? NumeroFacturaInterna { get; set; }
        public string? NumeroPedidoInterno { get; set; }
        public string? ZonaVenta { get; set; }
        public string? RutaVenta { get; set; }
        public string? InformacionAdicionalEmisor { get; set; }
        public string? FechaEmision { get; set; }
    }

    public class Comprador
    {
        public string? RNCComprador { get; set; }
        public string? IdentificadorExtranjero { get; set; }
        public string? RazonSocialComprador { get; set; }
        
        public string? ContactoComprador { get; set; }
        public string? CorreoComprador { get; set; }
        public string? DireccionComprador { get; set; }
        public string? MunicipioComprador { get; set; }
        public string? ProvinciaComprador { get; set; }
        public string? FechaEntrega { get; set; }
        public string? ContactoEntrega { get; set; }
        public string? DireccionEntrega { get; set; }
        public string? TelefonoAdicional { get; set; }
        public string? FechaOrdenCompra { get; set; }
        public string? NumeroOrdenCompra { get; set; }
        public string? CodigoInternoComprador { get; set; }
        public string? ResponsablePago { get; set; }
        public string? InformacionAdicionalComprador { get; set; }
    }

    public class OtraMoneda
    {
        public string? TipoMoneda { get; set; }
        public string? TipoCambio { get; set; }
        public string? MontoGravadoTotalOtraMoneda { get; set; }
        public string? MontoGravado1OtraMoneda { get; set; }
        public string? MontoGravado2OtraMoneda { get; set; }
        public string? MontoGravado3OtraMoneda { get; set; }
        public string? MontoExentoOtraMoneda { get; set; }
        public string? TotalITBISOtraMoneda { get; set; }
        public string? TotalITBIS1OtraMoneda { get; set; }
        public string? TotalITBIS2OtraMoneda { get; set; }
        public string? TotalITBIS3OtraMoneda { get; set; }
        public string? MontoImpuestoAdicionalOtraMoneda { get; set; }
        public List<ImpuestoAdicionalOtraMoneda>? ImpuestosAdicionalesOtraMoneda { get; set; }
        public string? MontoTotalOtraMoneda { get; set; }
    }

    public class InformacionesAdicionales
    {
        public string? FechaEmbarque { get; set; }
        public string? NumeroEmbarque { get; set; }
        public string? NumeroContenedor { get; set; }
        public string? NumeroReferencia { get; set; }
        public string? PesoBruto { get; set; }
        public string? PesoNeto { get; set; }
        public string? UnidadPesoBruto { get; set; }
        public string? UnidadPesoNeto { get; set; }
        public string? CantidadBulto { get; set; }
        public string? UnidadBulto { get; set; }
        public string? VolumenBulto { get; set; }
        public string? UnidadVolumen { get; set; }

    }

    public class Transporte
    {
        public string? Conductor { get; set; }
        public string? DocumentoTransporte { get; set; }
        public string? Ficha { get; set; }
        public string? Placa { get; set; }
        public string? RutaTransporte { get; set; }
        public string? ZonaTransporte { get; set; }
        public string? NumeroAlbaran { get; set; }

    }

    public class Totales
    {
        public string? MontoGravadoTotal { get; set; }
        public string? MontoGravadoI1 { get; set; }
        public string? MontoGravadoI2 { get; set; }
        public string? MontoGravadoI3 { get; set; }
        public string? MontoExento { get; set; }
        public string? ITBIS1 { get; set; }
        public string? ITBIS2 { get; set; }
        public string? ITBIS3 { get; set; }
        public string? TotalITBIS { get; set; }
        public string? TotalITBIS1 { get; set; }
        public string? TotalITBIS2 { get; set; }
        public string? TotalITBIS3 { get; set; }
        public string? MontoImpuestoAdicional { get; set; }
        public List<ImpuestoAdicional>? ImpuestosAdicionales { get; set; }
        public string? MontoTotal { get; set; }
        public string? MontoNoFacturable { get; set; }
        public string? MontoPeriodo { get; set; }
        public string? SaldoAnterior { get; set; }
        public string? MontoAvancePago { get; set; }
        public string? ValorPagar { get; set; }
        public string? TotalITBISRetenido { get; set; }
        public string? TotalISRRetencion { get; set; }
        public string? TotalITBISPercepcion { get; set; }
        public string? TotalISRPercepcion { get; set; }

    }

    public class Item
    {
        public string? NumeroLinea { get; set; }

        [XmlArray("TablaCodigosItem")]
        [XmlArrayItem("CodigosItem")]
        public List<CodigosItem>? TablaCodigosItem { get; set; }

        public string? IndicadorFacturacion { get; set; }
        public Retencion? Retencion { get; set; }
        public string? NombreItem { get; set; }
        public string? IndicadorBienoServicio { get; set; }
        public string? DescripcionItem { get; set; }
        public string? CantidadItem { get; set; }
        public string? UnidadMedida { get; set; }
        public string? CantidadReferencia { get; set; }
        public string? UnidadReferencia { get; set; }

        [XmlArray("TablaSubcantidad")]
        [XmlArrayItem("SubcantidadItem")]
        public List<SubcantidadItem>? TablaSubcantidad { get; set; }

        public string? GradosAlcohol { get; set; }
        public string? PrecioUnitarioReferencia { get; set; }
        public string? FechaElaboracion { get; set; }
        public string? FechaVencimientoItem { get; set; }
        public Mineria? Mineria { get; set; }
        public string? PrecioUnitarioItem { get; set; }
        public string? DescuentoMonto { get; set; }

        [XmlArray("TablaSubDescuento")]
        [XmlArrayItem("SubDescuento")]
        public List<SubDescuento>? TablaSubDescuento { get; set; }

        public string? RecargoMonto { get; set; }

        [XmlArray("TablaSubRecargo")]
        [XmlArrayItem("SubRecargo")]
        public List<SubRecargo>? TablaSubRecargo { get; set; }

        public List<ImpuestoAdicional>? TablaImpuestoAdicional { get; set; }
        public OtraMonedaDetalle? OtraMonedaDetalle { get; set; }
        public string? MontoItem { get; set; }

    }

    public class CodigosItem
    {
        public string? TipoCodigo { get; set; }
        public string? CodigoItem { get; set; }
    }

    public class Retencion
    {
        public string? IndicadorAgenteRetencionoPercepcion { get; set; }
        public string? MontoITBISRetenido { get; set; }
        public string? MontoISRRetenido { get; set; }
    }

    public class SubcantidadItem
    {
        public string? Subcantidad { get; set; }
        public string? CodigoSubcantidad { get; set; }
    }

    public class SubDescuento
    {
        public string? TipoSubDescuento { get; set; }
        public string? SubDescuentoPorcentaje { get; set; }
        public string? MontoSubDescuento { get; set; }
    }

    public class SubRecargo
    {
        public string? TipoSubRecargo { get; set; }
        public string? SubRecargoPorcentaje { get; set; }
        public string? MontoSubRecargo { get; set; }
    }

    public class ImpuestoAdicional
    {
        public string? TipoImpuesto { get; set; }
        public string? TasaImpuestoAdicional { get; set; }
        public string? MontoImpuestoSelectivoConsumoEspecifico { get; set; }
        public string? MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
        public string? OtrosImpuestosAdicionales { get; set; }
    }


    public class OtraMonedaDetalle
    {
        public string? PrecioOtraMoneda { get; set; }
        public string? DescuentoOtraMoneda { get; set; }
        public string? RecargoOtraMoneda { get; set; }
        public string? MontoItemOtraMoneda { get; set; }
    }

    public class Subtotal
    {
        public string? NumeroSubTotal { get; set; }
        public string? DescripcionSubtotal { get; set; }
        public string? Orden { get; set; }
        public string? SubTotalMontoGravadoTotal { get; set; }
        public string? SubTotalMontoGravadoI1 { get; set; }
        public string? SubTotalMontoGravadoI2 { get; set; }
        public string? SubTotalMontoGravadoI3 { get; set; }
        public string? SubTotaITBIS { get; set; }
        public string? SubTotaITBIS1 { get; set; }
        public string? SubTotaITBIS2 { get; set; }
        public string? SubTotaITBIS3 { get; set; }
        public string? SubTotalImpuestoAdicional { get; set; }
        public string? SubTotalExento { get; set; }
        public string? MontoSubTotal { get; set; }
        public string? Lineas { get; set; }
    }

    public class DescuentoORecargo
    {
        public string? NumeroLinea { get; set; }
        public string? TipoAjuste { get; set; }
        public string? IndicadorNorma1007 { get; set; }
        public string? DescripcionDescuentooRecargo { get; set; }
        public string? TipoValor { get; set; }
        public string? ValorDescuentooRecargo { get; set; }
        public string? MontoDescuentooRecargo { get; set; }
        public string? MontoDescuentooRecargoOtraMoneda { get; set; }
        public string? IndicadorFacturacionDescuentooRecargo { get; set; }
    }

    public class ImpuestoAdicionalOtraMoneda
    {
        public string? TipoImpuestoOtraMoneda { get; set; }
        public string? TasaImpuestoAdicionalOtraMoneda { get; set; }
        public string? MontoImpuestoSelectivoConsumoEspecificoOtraMoneda { get; set; }
        public string? MontoImpuestoSelectivoConsumoAdvaloremOtraMoneda { get; set; }
        public string? OtrosImpuestosAdicionalesOtraMoneda { get; set; }
    }

    public class Pagina
    {
        public string? PaginaNo { get; set; }
        public string? NoLineaDesde { get; set; }
        public string? NoLineaHasta { get; set; }
        public string? SubtotalMontoGravadoPagina { get; set; }
        public string? SubtotalMontoGravado1Pagina { get; set; }
        public string? SubtotalMontoGravado2Pagina { get; set; }
        public string? SubtotalMontoGravado3Pagina { get; set; }
        public string? SubtotalExentoPagina { get; set; }
        public string? SubtotalItbisPagina { get; set; }
        public string? SubtotalItbis1Pagina { get; set; }
        public string? SubtotalItbis2Pagina { get; set; }
        public string? SubtotalItbis3Pagina { get; set; }
        public string? SubtotalImpuestoAdicionalPagina { get; set; }
        public SubtotalImpuestoAdicional? SubtotalImpuestoAdicional { get; set; }
        public string? MontoSubtotalPagina { get; set; }
        public string? SubtotalMontoNoFacturablePagina { get; set; }
    }

    public class SubtotalImpuestoAdicional
    {
        public string? SubtotalImpuestoSelectivoConsumoEspecificoPagina { get; set; }
        public string? SubtotalOtrosImpuesto { get; set; }
    }

    public class InformacionReferencia
    {
        public string? NCFModificado { get; set; }
        public string? RNCOtroContribuyente { get; set; }
        public string? FechaNCFModificado { get; set; }
        public string? CodigoModificacion { get; set; }
        public string? RazonModificacion { get; set; }
    }

    // Exclusiva para el tipo de factura
    public class Mineria
    {
        public string? PesoNetoKilogramo { get; set; }
        public string? PesoNetoMineria { get; set; }
        public string? TipoAfiliacion { get; set; }
        public string? Liquidacion { get; set; }
    }


}
