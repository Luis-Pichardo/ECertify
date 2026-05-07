using OfficeOpenXml;
using eCertify.DTOs;
using eCertify.Interfaces;
using eCertify.Models;
using eCertify.Utils;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using eCertify.Data;

namespace eCertify.Services
{
    public class FacturasXmlService : IFacturasXmlService
    {
        private readonly ISemillaService _semillaService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FacturasXmlService> _logger;
        private readonly SogeDbContext _dbContext;

        public FacturasXmlService(ISemillaService semillaService, IHttpClientFactory httpClientFactory, ILogger<FacturasXmlService> logger, SogeDbContext dbContext)
        {
            _semillaService = semillaService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _dbContext = dbContext;
        }
        public async Task<string> GenerarXmlDesdeModeloAsync(FacturasModels factura)
        {
            // Solo genera el XML sin guardarlo
            XElement xml = new("ECF",
                CrearElementoDesdeObjeto("Encabezado", factura.Encabezado),
                CrearListaDesdeObjeto("DetallesItems", "Item", factura.DetallesItems),
                CrearListaDesdeObjeto("Subtotales", "Subtotal", factura.Subtotales),
                CrearListaDesdeObjeto("DescuentosORecargos", "DescuentoORecargo", factura.DescuentosORecargos),
                CrearListaDesdeObjeto("Paginacion", "Pagina", factura.Paginacion),
                CrearElementoDesdeObjeto("InformacionReferencia", factura.InformacionReferencia),
                CrearElementoSimple("FechaHoraFirma", factura.FechaHoraFirma),
                CrearElementoSimple("any_element", factura.any_element)
            );

            XDocument doc = new(new XDeclaration("1.0", "utf-8", "yes"), xml);
            return doc.ToString();
        }

        public async Task<string> GenerarXmlsDesdeExcelAsync(string rutaExcel)
        {
            if (!File.Exists(rutaExcel))
                throw new FileNotFoundException("Archivo Excel no encontrado", rutaExcel);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(new FileInfo(rutaExcel));
            var worksheet = package.Workbook.Worksheets["ECF"];
            if (worksheet == null)
                throw new Exception("Hoja 'ECF' no encontrada");

            var header = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var colName = worksheet.Cells[1, col].Text.Trim();
                if (!string.IsNullOrEmpty(colName))
                    header[colName] = col;
            }

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in header)
                {
                    var val = worksheet.Cells[row, kvp.Value].Text?.Trim();
                    if (!string.IsNullOrEmpty(val) && val != "#e")
                        rowData[kvp.Key] = val;
                }

                var model = MapearFacturaDesdeDiccionario(rowData);
                var encf = model?.Encabezado?.IdDoc?.eNCF;

                if (!string.IsNullOrWhiteSpace(encf) && encf != "#e")
                    GuardarXml(model);
            }

            return "XMLs generados desde Excel";
        }

        private void GuardarXml(FacturasModels factura)
        {
            XElement xml = new("ECF",
                CrearElementoDesdeObjeto("Encabezado", factura.Encabezado),
                CrearListaDesdeObjeto("DetallesItems", "Item", factura.DetallesItems),
                CrearListaDesdeObjeto("Subtotales", "Subtotal", factura.Subtotales),
                CrearListaDesdeObjeto("DescuentosORecargos", "DescuentoORecargo", factura.DescuentosORecargos),
                CrearListaDesdeObjeto("Paginacion", "Pagina", factura.Paginacion),
                CrearElementoDesdeObjeto("InformacionReferencia", factura.InformacionReferencia),
                CrearElementoSimple("FechaHoraFirma", factura.FechaHoraFirma),
                CrearElementoSimple("any_element", factura.any_element)
            );

            XDocument doc = new(new XDeclaration("1.0", "utf-8", "yes"), xml);

            var rnc = factura.Encabezado?.Emisor?.RNCEmisor ?? "SINRNC";
            var encf = factura.Encabezado?.IdDoc?.eNCF ?? "SINENC";
            var nombreArchivo = $"{rnc}_{encf}.xml";
            var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "XMLGenerados", rnc);
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);
            doc.Save(rutaCompleta);
        }

        public FacturasModels MapearFacturaDesdeDiccionario(Dictionary<string, string> data)
        {
            var factura = new FacturasModels
            {
                Encabezado = new Encabezado
                {
                    Version = Get(data, "Version"),
                    IdDoc = new IdDoc
                    {
                        TipoeCF = Get(data, "TipoeCF"),
                        eNCF = Get(data, "eNCF"),
                        IndicadorNotaCredito = Get(data, "IndicadorNotaCredito"),
                        FechaVencimientoSecuencia = Get(data, "FechaVencimientoSecuencia"),
                        IndicadorEnvioDiferido = Get(data, "IndicadorEnvioDiferido"),
                        IndicadorMontoGravado = Get(data, "IndicadorMontoGravado"),
                        IndicadorServicioTodoIncluido = Get(data, "IndicadorServicioTodoIncluido"),
                        TipoIngresos = Get(data, "TipoIngresos"),
                        TipoPago = Get(data, "TipoPago"),
                        FechaLimitePago = Get(data, "FechaLimitePago"),
                        TerminoPago = Get(data, "TerminoPago"),
                        TablaFormasPago = Enumerable.Range(1, 10)
                            .Select(i => new FormaDePago
                            {
                                FormaPago = Get(data, $"FormaPago[{i}]"),
                                MontoPago = Get(data, $"MontoPago[{i}]")
                            })
                            .Where(p => !string.IsNullOrWhiteSpace(p.FormaPago))
                            .ToList(),
                        TipoCuentaPago = Get(data, "TipoCuentaPago"),
                        NumeroCuentaPago = Get(data, "NumeroCuentaPago"),
                        BancoPago = Get(data, "BancoPago"),
                        FechaDesde = Get(data, "FechaDesde"),
                        FechaHasta = Get(data, "FechaHasta"),
                        TotalPaginas = Get(data, "TotalPaginas")
                    },
                    Emisor = new Emisor
                    {
                        RNCEmisor = Get(data, "RNCEmisor"),
                        RazonSocialEmisor = Get(data, "RazonSocialEmisor"),
                        NombreComercial = Get(data, "NombreComercial"),
                        DireccionEmisor = Get(data, "DireccionEmisor"),
                        Municipio = Get(data, "Municipio"),
                        Provincia = Get(data, "Provincia"),
                        TablaTelefonoEmisor = new List<string>
                        {
                            Get(data, "TelefonoEmisor"),
                            Get(data, "TelefonoEmisor2"),
                            Get(data, "TelefonoEmisor3")
                        }.Where(t => !string.IsNullOrEmpty(t)).ToList(),
                        CorreoEmisor = Get(data, "CorreoEmisor"),
                        WebSite = Get(data, "WebSite"),
                        ActividadEconomica = Get(data, "ActividadEconomica"),
                        CodigoVendedor = Get(data, "CodigoVendedor"),
                        NumeroFacturaInterna = Get(data, "NumeroFacturaInterna"),
                        NumeroPedidoInterno = Get(data, "NumeroPedidoInterno"),
                        ZonaVenta = Get(data, "ZonaVenta"),
                        RutaVenta = Get(data, "RutaVenta"),
                        InformacionAdicionalEmisor = Get(data, "InformacionAdicionalEmisor"),
                        FechaEmision = Get(data, "FechaEmision")
                    },
                    Comprador = new Comprador
                    {
                        RNCComprador = Get(data, "RNCComprador"),
                        IdentificadorExtranjero = Get(data, "IdentificadorExtranjero"),
                        RazonSocialComprador = Get(data, "RazonSocialComprador"),
                        ContactoComprador = Get(data, "ContactoComprador"),
                        CorreoComprador = Get(data, "CorreoComprador"),
                        DireccionComprador = Get(data, "DireccionComprador"),
                        MunicipioComprador = Get(data, "MunicipioComprador"),
                        ProvinciaComprador = Get(data, "ProvinciaComprador"),
                        FechaEntrega = Get(data, "FechaEntrega"),
                        ContactoEntrega = Get(data, "ContactoEntrega"),
                        DireccionEntrega = Get(data, "DireccionEntrega"),
                        TelefonoAdicional = Get(data, "TelefonoAdicional"),
                        FechaOrdenCompra = Get(data, "FechaOrdenCompra"),
                        NumeroOrdenCompra = Get(data, "NumeroOrdenCompra"),
                        CodigoInternoComprador = Get(data, "CodigoInternoComprador"),
                        ResponsablePago = Get(data, "ResponsablePago"),
                        InformacionAdicionalComprador = Get(data, "InformacionAdicionalComprador")
                    },
                    
                    InformacionesAdicionales = new InformacionesAdicionales
                    {
                        FechaEmbarque = Get(data, "FechaEmbarque"),
                        NumeroEmbarque = Get(data, "NumeroEmbarque"),
                        NumeroContenedor = Get(data, "NumeroContenedor"),
                        NumeroReferencia = Get(data, "NumeroReferencia"),
                        PesoBruto = Get(data, "PesoBruto"),
                        PesoNeto = Get(data, "PesoNeto"),
                        UnidadPesoBruto = Get(data, "UnidadPesoBruto"),
                        UnidadPesoNeto = Get(data, "UnidadPesoNeto"),
                        CantidadBulto = Get(data, "CantidadBulto"),
                        UnidadBulto = Get(data, "UnidadBulto"),
                        VolumenBulto = Get(data, "VolumenBulto"),
                        UnidadVolumen = Get(data, "UnidadVolumen")
                    },
                    Transporte = new Transporte
                    {
                        Conductor = Get(data, "Conductor"),
                        DocumentoTransporte = Get(data, "DocumentoTransporte"),
                        Ficha = Get(data, "Ficha"),
                        Placa = Get(data, "Placa"),
                        RutaTransporte = Get(data, "RutaTransporte"),
                        ZonaTransporte = Get(data, "ZonaTransporte"),
                        NumeroAlbaran = Get(data, "NumeroAlbaran")
                    },
                    Totales = new Totales
                    {
                        MontoGravadoTotal = Get(data, "MontoGravadoTotal"),
                        MontoGravadoI1 = Get(data, "MontoGravadoI1"),
                        MontoGravadoI2 = Get(data, "MontoGravadoI2"),
                        MontoGravadoI3 = Get(data, "MontoGravadoI3"),
                        MontoExento = Get(data, "MontoExento"),
                        ITBIS1 = Get(data, "ITBIS1"),
                        ITBIS2 = Get(data, "ITBIS2"),
                        ITBIS3 = Get(data, "ITBIS3"),
                        TotalITBIS = Get(data, "TotalITBIS"),
                        TotalITBIS1 = Get(data, "TotalITBIS1"),
                        TotalITBIS2 = Get(data, "TotalITBIS2"),
                        TotalITBIS3 = Get(data, "TotalITBIS3"),
                        MontoImpuestoAdicional = Get(data, "MontoImpuestoAdicional"),
                        ImpuestosAdicionales = Enumerable.Range(1, 10)
                            .Select(i => new ImpuestoAdicional
                            {
                                TipoImpuesto = Get(data, $"TipoImpuesto[{i}]"),
                                TasaImpuestoAdicional = Get(data, $"TasaImpuestoAdicional[{i}]"),
                                MontoImpuestoSelectivoConsumoEspecifico = Get(data, $"MontoImpuestoSelectivoConsumoEspecifico[{i}]"),
                                MontoImpuestoSelectivoConsumoAdvalorem = Get(data, $"MontoImpuestoSelectivoConsumoAdvalorem[{i}]"),
                                OtrosImpuestosAdicionales = Get(data, $"OtrosImpuestosAdicionales[{i}]")
                            })
                            .Where(p => !string.IsNullOrWhiteSpace(p.TipoImpuesto))
                            .ToList(),
                        MontoTotal = Get(data, "MontoTotal"),
                        MontoNoFacturable = Get(data, "MontoNoFacturable"),
                        MontoPeriodo = Get(data, "MontoPeriodo"),
                        SaldoAnterior = Get(data, "SaldoAnterior"),
                        MontoAvancePago = Get(data, "MontoAvancePago"),
                        ValorPagar = Get(data, "ValorPagar"),
                        TotalITBISRetenido = Get(data, "TotalITBISRetenido"),
                        TotalISRRetencion = Get(data, "TotalISRRetencion"),
                        TotalITBISPercepcion = Get(data, "TotalITBISPercepcion"),
                        TotalISRPercepcion = Get(data, "TotalISRPercepcion")
                    },
                    OtraMoneda = new OtraMoneda
                    {
                        TipoMoneda = Get(data, "TipoMoneda"),
                        TipoCambio = Get(data, "TipoCambio"),
                        MontoGravadoTotalOtraMoneda = Get(data, "MontoGravadoTotalOtraMoneda"),
                        MontoGravado1OtraMoneda = Get(data, "MontoGravado1OtraMoneda"),
                        MontoGravado2OtraMoneda = Get(data, "MontoGravado2OtraMoneda"),
                        MontoGravado3OtraMoneda = Get(data, "MontoGravado3OtraMoneda"),
                        MontoExentoOtraMoneda = Get(data, "MontoExentoOtraMoneda"),
                        TotalITBISOtraMoneda = Get(data, "TotalITBISOtraMoneda"),
                        TotalITBIS1OtraMoneda = Get(data, "TotalITBIS1OtraMoneda"),
                        TotalITBIS2OtraMoneda = Get(data, "TotalITBIS2OtraMoneda"),
                        TotalITBIS3OtraMoneda = Get(data, "TotalITBIS3OtraMoneda"),
                        MontoImpuestoAdicionalOtraMoneda = Get(data, "MontoImpuestoAdicionalOtraMoneda"),
                        ImpuestosAdicionalesOtraMoneda = Enumerable.Range(1, 10)
                        .Select(i => new ImpuestoAdicionalOtraMoneda
                        {
                            TipoImpuestoOtraMoneda = Get(data, $"TipoImpuestoOtraMoneda[{i}]"),
                            TasaImpuestoAdicionalOtraMoneda = Get(data, $"TasaImpuestoAdicionalOtraMoneda[{i}]"),
                            MontoImpuestoSelectivoConsumoEspecificoOtraMoneda = Get(data, $"MontoImpuestoSelectivoConsumoEspecificoOtraMoneda[{i}]"),
                            MontoImpuestoSelectivoConsumoAdvaloremOtraMoneda = Get(data, $"MontoImpuestoSelectivoConsumoAdvaloremOtraMoneda[{i}]"),
                            OtrosImpuestosAdicionalesOtraMoneda = Get(data, $"OtrosImpuestosAdicionalesOtraMoneda[{i}]")
                        })
                        .Where(p => !string.IsNullOrWhiteSpace(p.TipoImpuestoOtraMoneda))
                        .ToList(),
                        MontoTotalOtraMoneda = Get(data, "MontoTotalOtraMoneda")
                    },

                },
            DetallesItems = Enumerable.Range(1, 20)
            .Select(i => new Item
            {
                NumeroLinea = Get(data, $"NumeroLinea[{i}]") ?? i.ToString(),
                TablaCodigosItem = Enumerable.Range(1, 3)
                .Select(c => new CodigosItem
                {
                    TipoCodigo = Get(data, $"TipoCodigo[{i}][{c}]"),
                    CodigoItem = Get(data, $"CodigoItem[{i}][{c}]")
                })
                .Where(c => !string.IsNullOrWhiteSpace(c.TipoCodigo) || !string.IsNullOrWhiteSpace(c.CodigoItem))
                .ToList(),
                IndicadorFacturacion = Get(data, $"IndicadorFacturacion[{i}]"),
                Retencion = new Retencion
                {
                    IndicadorAgenteRetencionoPercepcion = Get(data, $"IndicadorAgenteRetencionoPercepcion[{i}]"),
                    MontoITBISRetenido = Get(data, $"MontoITBISRetenido[{i}]"),
                    MontoISRRetenido = Get(data, $"MontoISRRetenido[{i}]")
                },
                NombreItem = Get(data, $"NombreItem[{i}]"),
                IndicadorBienoServicio = Get(data, $"IndicadorBienoServicio[{i}]"),
                DescripcionItem = Get(data, $"DescripcionItem[{i}]"),
                CantidadItem = Get(data, $"CantidadItem[{i}]"),
                UnidadMedida = Get(data, $"UnidadMedida[{i}]"),
                CantidadReferencia = Get(data, $"CantidadReferencia[{i}]"),
                UnidadReferencia = Get(data, $"UnidadReferencia[{i}]"),
                TablaSubcantidad = Enumerable.Range(1, 3)
                .Select(s => new SubcantidadItem
                {
                    Subcantidad = Get(data, $"Subcantidad[{i}][{s}]"),
                    CodigoSubcantidad = Get(data, $"CodigoSubcantidad[{i}][{s}]")
                })
                .Where(s => !string.IsNullOrWhiteSpace(s.Subcantidad) || !string.IsNullOrWhiteSpace(s.CodigoSubcantidad))
                .ToList(),

                GradosAlcohol = Get(data, $"GradosAlcohol[{i}]"),
                PrecioUnitarioReferencia = Get(data, $"PrecioUnitarioReferencia[{i}]"),
                FechaElaboracion = Get(data, $"FechaElaboracion[{i}]"),
                FechaVencimientoItem = Get(data, $"FechaVencimientoItem[{i}]"),
                Mineria = new Mineria
                {
                    PesoNetoKilogramo = Get(data, $"PesoNetoKilogramo[{i}]"),
                    PesoNetoMineria = Get(data, $"PesoNetoMineria[{i}]"),
                    TipoAfiliacion = Get(data, $"TipoAfiliacion[{i}]"),
                    Liquidacion = Get(data, $"Liquidacion[{i}]")
                },
                PrecioUnitarioItem = Get(data, $"PrecioUnitarioItem[{i}]"),
                DescuentoMonto = Get(data, $"DescuentoMonto[{i}]"),
                TablaSubDescuento = Enumerable.Range(1, 10)
                .Select(d =>
                {
                    var tipo = Get(data, $"TipoSubDescuento[{i}][{d}]");
                    var porcentajeStr = Get(data, $"SubDescuentoPorcentaje[{i}][{d}]");
                    var montoStr = Get(data, $"MontoSubDescuento[{i}][{d}]");
            
                    // Solo parsear si no está vacío
                    decimal? porcentaje = null;
                    decimal? monto = null;
            
                    if (decimal.TryParse(porcentajeStr, out var p)) porcentaje = p;
                    if (decimal.TryParse(montoStr, out var m)) monto = m;

                    return new SubDescuento
                    {
                        TipoSubDescuento = tipo,
                        SubDescuentoPorcentaje = porcentaje?.ToString(),
                        MontoSubDescuento = monto?.ToString()
                    };
                })
                .Where(sd =>
                    !string.IsNullOrWhiteSpace(sd.TipoSubDescuento) ||
                    !string.IsNullOrWhiteSpace(sd.SubDescuentoPorcentaje) ||
                    !string.IsNullOrWhiteSpace(sd.MontoSubDescuento)
                )
                .ToList(),

                RecargoMonto = Get(data, $"RecargoMonto[{i}]"),
                TablaSubRecargo = Enumerable.Range(1,5)
                    .Select(r => new SubRecargo
                    {
                        TipoSubRecargo = Get(data, $"TipoSubRecargo[{i}][{r}]"),
                        SubRecargoPorcentaje = Get(data, $"SubRecargoPorcentaje[{i}][{r}]"),
                        MontoSubRecargo = Get(data, $"MontoSubRecargo[{i}][{r}]")
                    })
                    .Where(r => !string.IsNullOrWhiteSpace(r.TipoSubRecargo) ||
                               !string.IsNullOrWhiteSpace(r.SubRecargoPorcentaje) ||
                               !string.IsNullOrWhiteSpace(r.MontoSubRecargo))
                    .ToList(),
                TablaImpuestoAdicional = Enumerable.Range(1, 6)
                .Select(t => {
                    var tipo = Get(data, $"TipoImpuesto[{i}][{t}]");
                    if (string.IsNullOrWhiteSpace(tipo)) return null;
            
                    return new ImpuestoAdicional
                    {
                        TipoImpuesto = tipo,
                        TasaImpuestoAdicional = Get(data, $"TasaImpuestoAdicional[{i}][{t}]"),
                        MontoImpuestoSelectivoConsumoEspecifico = Get(data, $"MontoImpuestoSelectivoConsumoEspecifico[{i}][{t}]"),
                        MontoImpuestoSelectivoConsumoAdvalorem = Get(data, $"MontoImpuestoSelectivoConsumoAdvalorem[{i}][{t}]"),
                        OtrosImpuestosAdicionales = Get(data, $"OtrosImpuestosAdicionales[{i}][{t}]")
                    };
                })
                .Where(t => t != null)
                .ToList(),


                OtraMonedaDetalle = new OtraMonedaDetalle
                {
                    PrecioOtraMoneda = Get(data, $"PrecioOtraMoneda[{i}]"),
                    DescuentoOtraMoneda = Get(data, $"DescuentoOtraMoneda[{i}]"),
                    RecargoOtraMoneda = Get(data, $"RecargoOtraMoneda[{i}]"),
                    MontoItemOtraMoneda = Get(data, $"MontoItemOtraMoneda[{i}]")
                },
                MontoItem = Get(data, $"MontoItem[{i}]")
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.NombreItem) ||
                          !string.IsNullOrWhiteSpace(item.DescripcionItem) ||
                          !string.IsNullOrWhiteSpace(item.CantidadItem))
            .ToList(),
                Subtotales = Enumerable.Range(1, 3)
            .Select(s => new Subtotal
            {
                NumeroSubTotal = Get(data, $"NumeroSubTotal[{s}]"),
                DescripcionSubtotal = Get(data, $"DescripcionSubtotal[{s}]"),
                Orden = Get(data, $"Orden[{s}]"),
                SubTotalMontoGravadoTotal = Get(data, $"SubTotalMontoGravadoTotal[{s}]"),
                SubTotalMontoGravadoI1 = Get(data, $"SubTotalMontoGravadoI1[{s}]"),
                SubTotalMontoGravadoI2 = Get(data, $"SubTotalMontoGravadoI2[{s}]"),
                SubTotalMontoGravadoI3 = Get(data, $"SubTotalMontoGravadoI3[{s}]"),
                SubTotaITBIS = Get(data, $"SubTotaITBIS[{s}]"),
                SubTotaITBIS1 = Get(data, $"SubTotaITBIS1[{s}]"),
                SubTotaITBIS2 = Get(data, $"SubTotaITBIS2[{s}]"),
                SubTotaITBIS3 = Get(data, $"SubTotaITBIS3[{s}]"),
                SubTotalImpuestoAdicional = Get(data, $"SubTotalImpuestoAdicional[{s}]"),
                SubTotalExento = Get(data, $"SubTotalExento[{s}]"),
                MontoSubTotal = Get(data, $"MontoSubTotal[{s}]"),
                Lineas = Get(data, $"Lineas[{s}]")
            })
            .Where(s => !string.IsNullOrWhiteSpace(s.NumeroSubTotal))
            .ToList(),
                DescuentosORecargos = Enumerable.Range(1, 3)
            .Select(d => new DescuentoORecargo
            {
                NumeroLinea = Get(data, $"NumeroLineaDoR[{d}]"),
                TipoAjuste = Get(data, $"TipoAjuste[{d}]"),
                IndicadorNorma1007 = Get(data, $"IndicadorNorma1007[{d}]"),
                DescripcionDescuentooRecargo = Get(data, $"DescripcionDescuentooRecargo[{d}]"),
                TipoValor = Get(data, $"TipoValor[{d}]"),
                ValorDescuentooRecargo = Get(data, $"ValorDescuentooRecargo[{d}]"),
                MontoDescuentooRecargo = Get(data, $"MontoDescuentooRecargo[{d}]"),
                MontoDescuentooRecargoOtraMoneda = Get(data, $"MontoDescuentooRecargoOtraMoneda[{d}]"),
                IndicadorFacturacionDescuentooRecargo = Get(data, $"IndicadorFacturacionDescuentooRecargo[{d}]")
            })
            .Where(d => !string.IsNullOrWhiteSpace(d.TipoAjuste))
            .ToList(),
                Paginacion = Enumerable.Range(1, 3)
            .Select(p => new Pagina
            {
                PaginaNo = Get(data, $"PaginaNo[{p}]"),
                NoLineaDesde = Get(data, $"NoLineaDesde[{p}]"),
                NoLineaHasta = Get(data, $"NoLineaHasta[{p}]"),
                SubtotalMontoGravadoPagina = Get(data, $"SubtotalMontoGravadoPagina[{p}]"),
                SubtotalMontoGravado1Pagina = Get(data, $"SubtotalMontoGravado1Pagina[{p}]"),
                SubtotalMontoGravado2Pagina = Get(data, $"SubtotalMontoGravado2Pagina[{p}]"),
                SubtotalMontoGravado3Pagina = Get(data, $"SubtotalMontoGravado3Pagina[{p}]"),
                SubtotalExentoPagina = Get(data, $"SubtotalExentoPagina[{p}]"),
                SubtotalItbisPagina = Get(data, $"SubtotalItbisPagina[{p}]"),
                SubtotalItbis1Pagina = Get(data, $"SubtotalItbis1Pagina[{p}]"),
                SubtotalItbis2Pagina = Get(data, $"SubtotalItbis2Pagina[{p}]"),
                SubtotalItbis3Pagina = Get(data, $"SubtotalItbis3Pagina[{p}]"),
                SubtotalImpuestoAdicionalPagina = Get(data, $"SubtotalImpuestoAdicionalPagina[{p}]"),
                SubtotalImpuestoAdicional = new SubtotalImpuestoAdicional
                {
                    SubtotalImpuestoSelectivoConsumoEspecificoPagina = Get(data, $"SubtotalImpuestoSelectivoConsumoEspecificoPagina[{p}]"),
                    SubtotalOtrosImpuesto = Get(data, $"SubtotalOtrosImpuesto[{p}]")
                },
                MontoSubtotalPagina = Get(data, $"MontoSubtotalPagina[{p}]"),
                SubtotalMontoNoFacturablePagina = Get(data, $"SubtotalMontoNoFacturablePagina[{p}]")
            })
            .Where(p => !string.IsNullOrWhiteSpace(p.PaginaNo))
            .ToList(),
                InformacionReferencia = new InformacionReferencia
                {
                    NCFModificado = Get(data, "NCFModificado"),
                    RNCOtroContribuyente = Get(data, "RNCOtroContribuyente"),
                    FechaNCFModificado = Get(data, "FechaNCFModificado"),
                    CodigoModificacion = Get(data, "CodigoModificacion"),
                    RazonModificacion = Get(data, "RazonModificacion"),
                },
                FechaHoraFirma = Get(data, "FechaHoraFirma"),
                any_element = Get(data, "any_element")

            };

            return factura;
        }


        private string Get(Dictionary<string, string> dict, string key)
        {
            // Búsqueda exacta
            if (dict.TryGetValue(key, out var exactValue))
                return string.IsNullOrWhiteSpace(exactValue) ? null : exactValue;

            // Búsqueda por patrón con [i][j]
            var match = dict.FirstOrDefault(kvp =>
                Regex.IsMatch(kvp.Key, $@"^{Regex.Escape(key)}\[\d+\]\[\d+\]$")
            );

            return string.IsNullOrWhiteSpace(match.Value) ? null : match.Value;
        }

        private XElement CrearElementoDesdeObjeto(string nombreElemento, object objeto)
        {
            if (objeto == null) return null;
            var contenido = CrearXmlDesdeObjeto(objeto);
            return contenido != null ? new XElement(nombreElemento, contenido.Elements()) : null;
        }

        private XElement CrearElementoSimple(string nombreElemento, string valor)
        {
            return string.IsNullOrWhiteSpace(valor) || valor == "#e" ? null : new XElement(nombreElemento, valor);
        }

        public XElement CrearXmlDesdeObjeto(object obj)
        {
            if (obj == null) return null;

            XElement elemento = new(obj.GetType().Name);
            foreach (var prop in obj.GetType().GetProperties())
            {
                var valor = prop.GetValue(obj);
                if (valor == null || (valor is string s && s == "#e")) continue;

                // Caso especial para TablaTelefonoEmisor
                if (prop.Name == "TablaTelefonoEmisor" && valor is List<string> telefonos)
                {
                    if (telefonos.Any())
                    {
                        XElement contenedor = new(prop.Name);
                        foreach (var telefono in telefonos.Where(t => !string.IsNullOrEmpty(t)))
                        {
                            contenedor.Add(new XElement("TelefonoEmisor", telefono));
                        }
                        elemento.Add(contenedor);
                    }
                    continue;
                }

                if (valor is System.Collections.IEnumerable enumerable && !(valor is string))
                {
                    XElement contenedor = new(prop.Name);
                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;
                        if (item.GetType().IsPrimitive || item is string)
                        {
                            contenedor.Add(new XElement(prop.Name.TrimEnd('s'), item));
                        }
                        else
                        {
                            var hijo = CrearXmlDesdeObjeto(item);
                            if (hijo != null) contenedor.Add(hijo);
                        }
                    }

                    if (contenedor.HasElements)
                        elemento.Add(contenedor);
                }
                else if (!prop.PropertyType.IsClass || valor is string)
                {
                    elemento.Add(new XElement(prop.Name, valor));
                }
                else
                {
                    var hijo = CrearXmlDesdeObjeto(valor);
                    if (hijo != null)
                        elemento.Add(new XElement(prop.Name, hijo.Elements()));
                }
            }
            return elemento.HasElements ? elemento : null;
        }

        private XElement CrearListaDesdeObjeto<T>(string nombreContenedor, string nombreItem, List<T> lista)
        {
            if (lista == null || !lista.Any()) return null;
            XElement contenedor = new(nombreContenedor);
            foreach (var item in lista)
            {
                var elemento = CrearXmlDesdeObjeto(item);
                if (elemento != null)
                    contenedor.Add(new XElement(nombreItem, elemento.Elements()));
            }
            return contenedor.HasElements ? contenedor : null;
        }

        //Nuevas funciones
        public Task ValidarFacturaAsync(FacturasModels factura)
        {
            // Validaciones síncronas
            if (factura?.Encabezado?.IdDoc == null)
                return Task.FromException(new Exception("La factura o IdDoc no puede ser nula."));

            if (string.IsNullOrWhiteSpace(factura.Encabezado?.IdDoc?.eNCF))
                return Task.FromException(new Exception("NCF inválido o faltante"));

            if (string.IsNullOrWhiteSpace(factura.Encabezado?.Emisor?.RNCEmisor))
                return Task.FromException(new Exception("RNC del emisor es obligatorio"));

            if (string.IsNullOrWhiteSpace(factura.Encabezado?.IdDoc?.TipoeCF))
                return Task.FromException(new Exception("El campo TipoeCF es obligatorio"));

            return Task.CompletedTask;
        }

        public (ExtraInfoFacturaDTO info, XmlDocument firmadoXml) ExtraerInformacionDesdeXml(XmlDocument xml, Empresa empresa)
        {
            var firmadoXml = _semillaService.FirmarXml(xml, empresa, true);
            var doc = new XmlDocument();
            doc.LoadXml(firmadoXml.OuterXml);

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

            var firmaNodo = doc.SelectSingleNode("//ds:SignatureValue", nsManager);
            var fechaNodo = doc.SelectSingleNode("//FechaHoraFirma");

            string firma = firmaNodo?.InnerText ?? "000000";
            string codigoSeguridad = firma.Length >= 6 ? firma.Substring(0, 6) : "000000";

            DateTime.TryParse(fechaNodo?.InnerText, out var fecha);
            fecha = fecha == default ? DateTime.Now : fecha;

            return (new ExtraInfoFacturaDTO
            {
                CodigoSeguridad = codigoSeguridad,
                FechaHoraFirma = fecha
            }, firmadoXml);
        }

        public string GenerarUrlQR(FacturasModels factura, ExtraInfoFacturaDTO info)
        {
            var emisor = factura.Encabezado.Emisor;
            var comprador = factura.Encabezado.Comprador;
            var idDoc = factura.Encabezado.IdDoc;
            var totales = factura.Encabezado.Totales;
            var tipoECF = idDoc?.TipoeCF;
            var montoTotal = Utils.Utils.ParseDecimalOrDefault(totales?.MontoTotal);

            if (tipoECF == "32" && montoTotal < 250000)
            {
                string urlQRResumen = "https://fc.dgii.gov.do/testecf/consultatimbrefc?" +
                    $"RncEmisor={Utils.Utils.ReemplazarCaracteresQR(emisor.RNCEmisor)}" +
                    $"&ENCF={Utils.Utils.ReemplazarCaracteresQR(idDoc.eNCF)}" +
                    $"&MontoTotal={totales?.MontoTotal?.Replace(",", ".") ?? ""}" +
                    $"&CodigoSeguridad={Utils.Utils.ReemplazarCaracteresQR(info.CodigoSeguridad)}";

                return urlQRResumen;
            }

            var rncComprador = comprador?.RNCComprador ?? comprador?.IdentificadorExtranjero ?? "";
            string urlQRFactura = $"{Constants.ConsultaTiembreProduccion}" +
                        $"RncEmisor={Utils.Utils.ReemplazarCaracteresQR(emisor.RNCEmisor)}" +
                        $"&RncComprador={Utils.Utils.ReemplazarCaracteresQR(rncComprador)}" +
                        $"&ENCF={Utils.Utils.ReemplazarCaracteresQR(idDoc.eNCF)}" +
                        $"&FechaEmision={Utils.Utils.ReemplazarCaracteresQR(emisor.FechaEmision)}" +
                        $"&MontoTotal={Utils.Utils.ReemplazarCaracteresQR(totales?.MontoTotal ?? "")}" +
                        $"&FechaFirma={Utils.Utils.ReemplazarCaracteresQR(info.FechaHoraFirma.ToString("dd-MM-yyyy HH:mm:ss"))}" +
                        $"&CodigoSeguridad={Utils.Utils.ReemplazarCaracteresQR(info.CodigoSeguridad)}";

            return urlQRFactura;
        }

        public async Task<(bool exito, string trackId, string mensaje)> EnviarFacturaADGIIAsync(string xmlFirmado, string fileName, string tipoECF, decimal montoTotal, string rncEmisor)
        {
            try
            {
                if (tipoECF == "32" && montoTotal < 250000)
                {
                    var clientResumen = _httpClientFactory.CreateClient("ApiClient");
                    var tokenResumen = await _semillaService.ObtenerTokenAsync(rncEmisor);
                    clientResumen.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResumen);

                    using var formDataResumen = new MultipartFormDataContent();
                    formDataResumen.Add(new StringContent(xmlFirmado, Encoding.UTF8, "application/xml"), "xml", fileName);

                    var responseResumen = await clientResumen.PostAsync(Constants.EnviarResumenProduccion, formDataResumen);
                    var contenidoResumen = await responseResumen.Content.ReadAsStringAsync();

                    if (!responseResumen.IsSuccessStatusCode)
                    {
                        _logger.LogError("Error al enviar el resumen a la DGII. Status: {StatusCode}, Respuesta: {Contenido}", responseResumen.StatusCode, contenidoResumen);
                        return (false, null, "Error al enviar el resumen a la DGII");
                    }

                    var trackIdResumen = Utils.Utils.ExtractTrackIdFromJson(contenidoResumen);
                    return (true, trackIdResumen, "Resumen enviado correctamente");
                }

                var token = await _semillaService.ObtenerTokenAsync(rncEmisor);
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogError("No se pudo obtener un token válido para el RNC {RNC}", rncEmisor);
                    return (false, null, "No se pudo obtener un token válido");
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(xmlFirmado, Encoding.UTF8, "application/xml"), "xml", fileName);

                var response = await client.PostAsync(Constants.EnviarFacturasProduccion, formData);
                var contenido = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error al enviar la factura a la DGII. Status: {StatusCode}, Respuesta: {Contenido}", response.StatusCode, contenido);
                    return (false, null, "Error al enviar la factura a la DGII");
                }

                var trackId = Utils.Utils.ExtractTrackIdFromJson(contenido);
                return (true, trackId, "Factura enviada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción inesperada al enviar factura/resumen a la DGII");
                return (false, null, "Ocurrió un error inesperado");
            }
        }

        public async Task<string> ConsultarEstadoDGIIAsync(string trackId, string rncEmisor)
        {
            if (string.IsNullOrWhiteSpace(trackId))
                throw new ArgumentException("El trackId no puede estar vacío");

            var token = await _semillaService.ObtenerTokenAsync(rncEmisor);
            if (string.IsNullOrWhiteSpace(token))
                throw new Exception("No se pudo obtener un token válido");

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var urlConsulta = $"{Constants.ConsultarFacturasProduccion}?trackId={trackId}";

            var response = await client.GetAsync(urlConsulta);
            var contenido = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error al consultar estado DGII. Código: {Code} | TrackId: {TrackId} | Respuesta: {Contenido}",
                                 response.StatusCode, trackId, contenido);
                throw new Exception("Error al consultar el estado de la factura en la DGII");
            }

            return contenido;
        }

        public async Task<bool> GuardarConsultasFacturaAsync(FacturaResponseDto respuesta)
        {
            try
            {
                // Validación completa de los datos requeridos
                if (respuesta?.RespuestaDGII == null)
                {
                    _logger.LogWarning("La respuesta DGII es nula");
                    return false;
                }

                // Validar TrackId
                if (respuesta.RespuestaDGII.TrackId == Guid.Empty)
                {
                    _logger.LogError($"TrackId no es válido: {respuesta.RespuestaDGII.TrackId}");
                    return false;
                }

                if (string.IsNullOrEmpty(respuesta.RespuestaDGII.ENCF))
                {
                    _logger.LogError("ENCF es requerido pero viene nulo o vacío");
                    return false;
                }

                // Configuración de formatos de fecha
                var formatosFecha = new[]
                {
                    "M/d/yyyy h:mm:ss tt",  // Formato de fechaRecepcion: "7/24/2025 4:20:32 PM"
                    "dd-MM-yyyy HH:mm:ss",   // Formato de fechaHoraFirma: "24-07-2025 16:20:26"
                    "dd/MM/yyyy HH:mm:ss",
                    "yyyy-MM-ddTHH:mm:ss",
                    "yyyy-MM-dd HH:mm:ss",
                    "M/d/yyyy",
                    "dd-MM-yyyy",
                    "yyyy-MM-dd"
                };

                // Parseo seguro de fechas
                DateTime fechaFirma;
                if (string.IsNullOrEmpty(respuesta.FechaHoraFirma))
                {
                    _logger.LogWarning("FechaHoraFirma está vacía, usando fecha actual");
                    fechaFirma = DateTime.Now;
                }
                else if (!DateTime.TryParseExact(respuesta.FechaHoraFirma, formatosFecha,
                         CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaFirma))
                {
                    _logger.LogWarning($"Formato de FechaHoraFirma no reconocido: {respuesta.FechaHoraFirma}");
                    fechaFirma = DateTime.Now;
                }

                DateTime fechaRecepcion;
                if (string.IsNullOrEmpty(respuesta.RespuestaDGII.FechaRecepcion))
                {
                    _logger.LogWarning("FechaRecepcion está vacía, usando fecha actual");
                    fechaRecepcion = DateTime.Now;
                }
                else if (!DateTime.TryParseExact(respuesta.RespuestaDGII.FechaRecepcion, formatosFecha,
                         CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaRecepcion))
                {
                    _logger.LogWarning($"Formato de FechaRecepcion no reconocido: {respuesta.RespuestaDGII.FechaRecepcion}");
                    fechaRecepcion = DateTime.Now;
                }

                // Creación del objeto historial
                var historial = new HistorialFacturas
                {
                    UrlQR = respuesta.UrlQR ?? string.Empty,
                    FechaHoraFirma = fechaFirma,
                    CodigoSeguridad = respuesta.CodigoSeguridad,
                    TrackId = respuesta.RespuestaDGII.TrackId,
                    CodigoRespuesta = respuesta.RespuestaDGII.Codigo,
                    Estado = respuesta.RespuestaDGII.Estado,
                    RncEmisor = respuesta.RespuestaDGII.Rnc,
                    RncComprador = respuesta.RncComprador,
                    RazonSocialComprador = respuesta.RazonSocialComprador,
                    ENCF = respuesta.RespuestaDGII.ENCF,
                    SecuenciaUtilizada = respuesta.RespuestaDGII.SecuenciaUtilizada,
                    FechaRecepcion = fechaRecepcion,
                    MensajeValor = respuesta.RespuestaDGII.Mensajes?.FirstOrDefault()?.Valor,
                    MensajeCodigo = respuesta.RespuestaDGII.Mensajes?.FirstOrDefault()?.Codigo ?? 0
                };

                // Guardado en base de datos
                await _dbContext.HistorialFacturas.AddAsync(historial);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar historial de factura. Respuesta: {@Respuesta}", respuesta);
                return false;
            }
        }
    }
}
