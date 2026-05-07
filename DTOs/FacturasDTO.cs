namespace eCertify.DTOs
{
    public class FacturasDTO
    {
        public EncabezadoDTO Encabezado { get; set; }
        public List<ItemDTO> DetallesItems { get; set; }
        public List<SubtotalDTO> Subtotales { get; set; }
        public List<DescuentoORecargoDTO> DescuentosORecargos { get; set; }
        public List<PaginaDTO> Paginacion { get; set; }
        public InformacionReferenciaDTO InformacionReferencia { get; set; }
        public string FechaHoraFirma { get; set; }
        public string AnyElement { get; set; }
    }

    public class EncabezadoDTO
    {
        public string Version { get; set; }
        public IdDocDTO IdDoc { get; set; }
        public EmisorDTO Emisor { get; set; }
        public CompradorDTO Comprador { get; set; }
        public InformacionesAdicionalesDTO InformacionesAdicionales { get; set; }
        public TransporteDTO Transporte { get; set; }
        public TotalesDTO Totales { get; set; }
        public OtraMonedaDTO OtraMoneda { get; set; }
    }

    public class IdDocDTO
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string IndicadorServicioTodoIncluido { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
        public string FechaLimitePago { get; set; }
        public string TerminoPago { get; set; }
        public List<FormaDePagoDTO> TablaFormasPago { get; set; }
        public string TipoCuentaPago { get; set; }
        public string NumeroCuentaPago { get; set; }
        public string BancoPago { get; set; }
        public string FechaDesde { get; set; }
        public string FechaHasta { get; set; }
        public string TotalPaginas { get; set; }
    }

    public class FormaDePagoDTO
    {
        public string FormaPago { get; set; }
        public string MontoPago { get; set; }
    }

    public class EmisorDTO
    {
        public string RNCEmisor { get; set; }
        public string RazonSocialEmisor { get; set; }
        public string NombreComercial { get; set; }
        public string Sucursal { get; set; }
        public string DireccionEmisor { get; set; }
        public string Municipio { get; set; }
        public string Provincia { get; set; }
        public List<string> TablaTelefonoEmisor { get; set; }
        public string CorreoEmisor { get; set; }
        public string WebSite { get; set; }
        public string ActividadEconomica { get; set; }
        public string CodigoVendedor { get; set; }
        public string NumeroFacturaInterna { get; set; }
        public string NumeroPedidoInterno { get; set; }
        public string ZonaVenta { get; set; }
        public string RutaVenta { get; set; }
        public string InformacionAdicionalEmisor { get; set; }
        public string FechaEmision { get; set; }
    }

    public class CompradorDTO
    {
        public string RNCComprador { get; set; }
        public string RazonSocialComprador { get; set; }
        public string ContactoComprador { get; set; }
        public string CorreoComprador { get; set; }
        public string DireccionComprador { get; set; }
        public string MunicipioComprador { get; set; }
        public string ProvinciaComprador { get; set; }
        public string FechaEntrega { get; set; }
        public string ContactoEntrega { get; set; }
        public string DireccionEntrega { get; set; }
        public string TelefonoAdicional { get; set; }
        public string FechaOrdenCompra { get; set; }
        public string NumeroOrdenCompra { get; set; }
        public string CodigoInternoComprador { get; set; }
        public string ResponsablePago { get; set; }
        public string InformacionAdicionalComprador { get; set; }
    }

    public class OtraMonedaDTO
    {
        public string TipoMoneda { get; set; }
        public string TipoCambio { get; set; }
        public string MontoGravadoTotalOtraMoneda { get; set; }
        public string MontoGravado1OtraMoneda { get; set; }
        public string MontoGravado2OtraMoneda { get; set; }
        public string MontoGravado3OtraMoneda { get; set; }
        public string MontoExentoOtraMoneda { get; set; }
        public string TotalITBISOtraMoneda { get; set; }
        public string TotalITBIS1OtraMoneda { get; set; }
        public string TotalITBIS2OtraMoneda { get; set; }
        public string TotalITBIS3OtraMoneda { get; set; }
        public string MontoImpuestoAdicionalOtraMoneda { get; set; }
        public List<ImpuestoAdicionalOtraMonedaDTO> ImpuestosAdicionalesOtraMoneda { get; set; }
        public string MontoTotalOtraMoneda { get; set; }
    }

    public class InformacionesAdicionalesDTO
    {
        public string FechaEmbarque { get; set; }
        public string NumeroEmbarque { get; set; }
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
        public string PesoBruto { get; set; }
        public string PesoNeto { get; set; }
        public string UnidadPesoBruto { get; set; }
        public string UnidadPesoNeto { get; set; }
        public string CantidadBulto { get; set; }
        public string UnidadBulto { get; set; }
        public string VolumenBulto { get; set; }
        public string UnidadVolumen { get; set; }
    }

    public class TransporteDTO
    {
        public string Conductor { get; set; }
        public string DocumentoTransporte { get; set; }
        public string Ficha { get; set; }
        public string Placa { get; set; }
        public string RutaTransporte { get; set; }
        public string ZonaTransporte { get; set; }
        public string NumeroAlbaran { get; set; }
    }

    public class TotalesDTO
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string MontoGravadoI2 { get; set; }
        public string MontoGravadoI3 { get; set; }
        public string MontoExento { get; set; }
        public string ITBIS1 { get; set; }
        public string ITBIS2 { get; set; }
        public string ITBIS3 { get; set; }
        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string TotalITBIS2 { get; set; }
        public string TotalITBIS3 { get; set; }
        public string MontoImpuestoAdicional { get; set; }
        public List<ImpuestoAdicionalDTO> ImpuestosAdicionales { get; set; }
        public string MontoTotal { get; set; }
        public string MontoNoFacturable { get; set; }
        public string MontoPeriodo { get; set; }
        public string SaldoAnterior { get; set; }
        public string MontoAvancePago { get; set; }
        public string ValorPagar { get; set; }
        public string TotalITBISRetenido { get; set; }
        public string TotalISRRetencion { get; set; }
        public string TotalITBISPercepcion { get; set; }
        public string TotalISRPercepcion { get; set; }
    }

    public class ItemDTO
    {
        public string NumeroLinea { get; set; }
        public List<CodigoItemDTO> TablaCodigosItem { get; set; }
        public string IndicadorFacturacion { get; set; }
        public RetencionDTO Retencion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string DescripcionItem { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string CantidadReferencia { get; set; }
        public string UnidadReferencia { get; set; }
        public List<SubcantidadItemDTO> TablaSubcantidad { get; set; }
        public string GradosAlcohol { get; set; }
        public string PrecioUnitarioReferencia { get; set; }
        public string FechaElaboracion { get; set; }
        public string FechaVencimientoItem { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string DescuentoMonto { get; set; }
        public List<SubDescuentoDTO> TablaSubDescuento { get; set; }
        public string RecargoMonto { get; set; }
        public List<SubRecargoDTO> TablaSubRecargo { get; set; }
        public List<ImpuestoAdicionalDTO> TablaImpuestoAdicional { get; set; }
        public OtraMonedaDetalleDTO OtraMonedaDetalle { get; set; }
        public string MontoItem { get; set; }
    }

    public class CodigoItemDTO
    {
        public string TipoCodigo { get; set; }
        public string CodigoItem { get; set; }
    }

    public class RetencionDTO
    {
        public string IndicadorAgenteRetencionoPercepcion { get; set; }
        public string MontoITBISRetenido { get; set; }
        public string MontoISRRetenido { get; set; }
    }

    public class SubcantidadItemDTO
    {
        public string Subcantidad { get; set; }
        public string CodigoSubcantidad { get; set; }
    }

    public class SubDescuentoDTO
    {
        public string TipoSubDescuento { get; set; }
        public string SubDescuentoPorcentaje { get; set; }
        public string MontoSubDescuento { get; set; }
    }

    public class SubRecargoDTO
    {
        public string TipoSubRecargo { get; set; }
        public string SubRecargoPorcentaje { get; set; }
        public string MontoSubRecargo { get; set; }
    }

    public class ImpuestoAdicionalDTO
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string MontoImpuestoSelectivoConsumoEspecifico { get; set; }
        public string MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class OtraMonedaDetalleDTO
    {
        public string PrecioOtraMoneda { get; set; }
        public string DescuentoOtraMoneda { get; set; }
        public string RecargoOtraMoneda { get; set; }
        public string MontoItemOtraMoneda { get; set; }
    }

    public class SubtotalDTO
    {
        public string NumeroSubTotal { get; set; }
        public string DescripcionSubtotal { get; set; }
        public string Orden { get; set; }
        public string SubTotalMontoGravadoTotal { get; set; }
        public string SubTotalMontoGravadoI1 { get; set; }
        public string SubTotalMontoGravadoI2 { get; set; }
        public string SubTotalMontoGravadoI3 { get; set; }
        public string SubTotaITBIS { get; set; }
        public string SubTotaITBIS1 { get; set; }
        public string SubTotaITBIS2 { get; set; }
        public string SubTotaITBIS3 { get; set; }
        public string SubTotalImpuestoAdicional { get; set; }
        public string SubTotalExento { get; set; }
        public string MontoSubTotal { get; set; }
        public string Lineas { get; set; }
    }

    public class DescuentoORecargoDTO
    {
        public string NumeroLinea { get; set; }
        public string TipoAjuste { get; set; }
        public string IndicadorNorma1007 { get; set; }
        public string DescripcionDescuentooRecargo { get; set; }
        public string TipoValor { get; set; }
        public string ValorDescuentooRecargo { get; set; }
        public string MontoDescuentooRecargo { get; set; }
        public string MontoDescuentooRecargoOtraMoneda { get; set; }
        public string IndicadorFacturacionDescuentooRecargo { get; set; }
    }

    public class ImpuestoAdicionalOtraMonedaDTO
    {
        public string TipoImpuestoOtraMoneda { get; set; }
        public string TasaImpuestoAdicionalOtraMoneda { get; set; }
        public string MontoImpuestoSelectivoConsumoEspecificoOtraMoneda { get; set; }
        public string MontoImpuestoSelectivoConsumoAdvaloremOtraMoneda { get; set; }
        public string OtrosImpuestosAdicionalesOtraMoneda { get; set; }
    }

    public class PaginaDTO
    {
        public string PaginaNo { get; set; }
        public string NoLineaDesde { get; set; }
        public string NoLineaHasta { get; set; }
        public string SubtotalMontoGravadoPagina { get; set; }
        public string SubtotalMontoGravado1Pagina { get; set; }
        public string SubtotalMontoGravado2Pagina { get; set; }
        public string SubtotalMontoGravado3Pagina { get; set; }
        public string SubtotalExentoPagina { get; set; }
        public string SubtotalItbisPagina { get; set; }
        public string SubtotalItbis1Pagina { get; set; }
        public string SubtotalItbis2Pagina { get; set; }
        public string SubtotalItbis3Pagina { get; set; }
        public string SubtotalImpuestoAdicionalPagina { get; set; }
        public SubtotalImpuestoAdicionalDTO SubtotalImpuestoAdicional { get; set; }
        public string MontoSubtotalPagina { get; set; }
        public string SubtotalMontoNoFacturablePagina { get; set; }
    }

    public class SubtotalImpuestoAdicionalDTO
    {
        public string SubtotalImpuestoSelectivoConsumoEspecificoPagina { get; set; }
        public string SubtotalOtrosImpuesto { get; set; }
    }

    public class InformacionReferenciaDTO
    {
        public string NCFModificado { get; set; }
        public string RNCOtroContribuyente { get; set; }
        public string FechaNCFModificado { get; set; }
        public string CodigoModificacion { get; set; }
    }

}
