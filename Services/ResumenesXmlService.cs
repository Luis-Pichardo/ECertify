
using eCertify.Interfaces;
using eCertify.Models.ResumenesModel;
using eCertify.Models;
using System.Globalization;
using System.Xml.Linq;

namespace eCertify.Services
{
    public class ResumenesXmlService : IResumenesXmlService
    {
        private readonly ILogger<ResumenesXmlService> _logger;

        public ResumenesXmlService(ILogger<ResumenesXmlService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerarXmlDesdeModeloAsync(ResumenesModel resumen)
        {
            XElement xml = new("RFCE",
                CrearElementoDesdeObjeto("Encabezado", resumen.Encabezado),
                CrearElementoSimple("any_element", resumen.AnyElement)
            );

            XDocument doc = new(new XDeclaration("1.0", "utf-8", "yes"), xml);
            return doc.ToString();
        }

        public ResumenesModel MapearResumenDesdeDiccionario(Dictionary<string, string> rowData)
        {
            try
            {
                if (rowData == null || !rowData.ContainsKey("eNCF"))
                {
                    _logger.LogWarning("Datos de resumen incompletos o sin eNCF");
                    return null;
                }

                // Mapeo de formas de pago si existen en el rowData
                var formasPago = new List<Models.ResumenesModel.FormaDePago>();
                if (rowData.TryGetValue("FormaPago", out var formaPago) &&
                    rowData.TryGetValue("MontoPago", out var montoPagoStr))
                {
                    formasPago.Add(new Models.ResumenesModel.FormaDePago
                    {
                        FormaPago = formaPago,
                        MontoPago = montoPagoStr
                    });
                }

                // Mapeo de impuestos adicionales si existen
                var impuestosAdicionales = new List<Models.ResumenesModel.ImpuestoAdicional>();
                if (rowData.TryGetValue("TipoImpuesto", out var tipoImpuesto))
                {
                    impuestosAdicionales.Add(new Models.ResumenesModel.ImpuestoAdicional
                    {
                        TipoImpuesto = tipoImpuesto,
                        MontoImpuestoSelectivoConsumoEspecifico = rowData.GetValueOrDefault("MontoImpuestoSelectivoConsumoEspecifico"),
                        MontoImpuestoSelectivoConsumoAdvalorem = rowData.GetValueOrDefault("MontoImpuestoSelectivoConsumoAdvalorem"),
                        OtrosImpuestosAdicionales = rowData.GetValueOrDefault("OtrosImpuestosAdicionales"),

                    });
                }

                return new ResumenesModel
                {
                    Encabezado = new Models.ResumenesModel.Encabezado
                    {
                        Version = rowData.GetValueOrDefault("Version") ?? "1.1",
                        IdDoc = new Models.ResumenesModel.IdDoc
                        {
                            TipoeCF = rowData.GetValueOrDefault("TipoeCF") ?? "E31",
                            eNCF = rowData["eNCF"],
                            TipoIngresos = rowData.GetValueOrDefault("TipoIngresos") ?? "01",
                            TipoPago = rowData.GetValueOrDefault("TipoPago"),
                            TablaFormasPago = formasPago.Count > 0 ? formasPago : null
                        },
                        Emisor = new Models.ResumenesModel.Emisor
                        {
                            RNCEmisor = rowData.GetValueOrDefault("RNCEmisor"),
                            RazonSocialEmisor = rowData.GetValueOrDefault("RazonSocialEmisor"),
                            FechaEmision = rowData.GetValueOrDefault("FechaEmision")

                        },
                        Comprador = new Models.ResumenesModel.Comprador
                        {
                            RNCComprador = rowData.GetValueOrDefault("RNCComprador"),
                            IdentificadorExtranjero = rowData.GetValueOrDefault("IdentificadorExtranjero"),
                            RazonSocialComprador = rowData.GetValueOrDefault("RazonSocialComprador")
                        },
                        Totales = new Models.ResumenesModel.Totales
                        {
                            MontoGravadoTotal = rowData.GetValueOrDefault("MontoGravadoTotal"),
                            MontoGravadoI1 = rowData.GetValueOrDefault("MontoGravadoI1"),
                            MontoGravadoI2 = rowData.GetValueOrDefault("MontoGravadoI2"),
                            MontoGravadoI3 = rowData.GetValueOrDefault("MontoGravadoI3"),
                            MontoExento = rowData.GetValueOrDefault("MontoExento"),
                            TotalITBIS = rowData.GetValueOrDefault("TotalITBIS"),
                            TotalITBIS1 = rowData.GetValueOrDefault("TotalITBIS1"),
                            TotalITBIS2 = rowData.GetValueOrDefault("TotalITBIS2"),
                            TotalITBIS3 = rowData.GetValueOrDefault("TotalITBIS3"),
                            MontoImpuestoAdicional = rowData.GetValueOrDefault("MontoImpuestoAdicional"),
                            ImpuestosAdicionales = impuestosAdicionales.Count > 0 ? new ImpuestosAdicionales
                            {
                                ImpuestoAdicional = impuestosAdicionales
                            } : null,
                            MontoTotal = rowData.GetValueOrDefault("MontoTotal"),
                            MontoNoFacturable = rowData.GetValueOrDefault("MontoNoFacturable"),
                            MontoPeriodo = rowData.GetValueOrDefault("MontoPeriodo")
                        },

                        CodigoSeguridadeCF = rowData.GetValueOrDefault("CodigoSeguridadeCF")
                    },
                    AnyElement = rowData.GetValueOrDefault("AnyElement")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mapear resumen desde diccionario");
                return null;
            }
        }

        public ResumenesModel MapearResumenDesdeFactura(FacturasModels factura)
        {
            return new ResumenesModel
            {
                Encabezado = new Models.ResumenesModel.Encabezado
                {
                    Version = factura.Encabezado?.Version,
                    IdDoc = new Models.ResumenesModel.IdDoc
                    {
                        TipoeCF = factura.Encabezado?.IdDoc?.TipoeCF,
                        eNCF = factura.Encabezado?.IdDoc?.eNCF,
                        TipoIngresos = factura.Encabezado?.IdDoc?.TipoIngresos,
                        TipoPago = factura.Encabezado?.IdDoc?.TipoPago,
                        TablaFormasPago = factura.Encabezado.IdDoc.TablaFormasPago?
                            .Select(fp => new Models.ResumenesModel.FormaDePago
                            {
                                FormaPago = fp.FormaPago,
                                MontoPago = fp.MontoPago
                            })
                            .ToList()
                    },
                    Emisor = new Models.ResumenesModel.Emisor
                    {
                        RNCEmisor = factura.Encabezado?.Emisor?.RNCEmisor,
                        RazonSocialEmisor = factura.Encabezado?.Emisor?.RazonSocialEmisor,
                        FechaEmision = factura.Encabezado?.Emisor?.FechaEmision
                    },
                    Comprador = new Models.ResumenesModel.Comprador
                    {
                        RNCComprador = factura.Encabezado?.Comprador?.RNCComprador,
                        IdentificadorExtranjero = factura.Encabezado?.Comprador?.IdentificadorExtranjero,
                        RazonSocialComprador = factura.Encabezado?.Comprador?.RazonSocialComprador
                    },
                    Totales = new Models.ResumenesModel.Totales
                    {
                        MontoGravadoTotal = factura.Encabezado?.Totales?.MontoGravadoTotal,
                        MontoGravadoI1 = factura.Encabezado?.Totales?.MontoGravadoI1,
                        MontoGravadoI2 = factura.Encabezado?.Totales?.MontoGravadoI2,
                        MontoGravadoI3 = factura.Encabezado?.Totales?.MontoGravadoI3,
                        MontoExento = factura.Encabezado?.Totales?.MontoExento,
                        TotalITBIS = factura.Encabezado?.Totales?.TotalITBIS,
                        TotalITBIS1 = factura.Encabezado?.Totales?.TotalITBIS1,
                        TotalITBIS2 = factura.Encabezado?.Totales?.TotalITBIS2,
                        TotalITBIS3 = factura.Encabezado?.Totales?.TotalITBIS3,
                        MontoImpuestoAdicional = factura.Encabezado?.Totales?.MontoImpuestoAdicional,
                        ImpuestosAdicionales = factura.Encabezado?.Totales?.ImpuestosAdicionales != null
                        ? new ImpuestosAdicionales
                        {
                            ImpuestoAdicional = factura.Encabezado.Totales.ImpuestosAdicionales
                                .Select(i => new Models.ResumenesModel.ImpuestoAdicional
                                {
                                    TipoImpuesto = i.TipoImpuesto,
                                    MontoImpuestoSelectivoConsumoEspecifico = i.MontoImpuestoSelectivoConsumoEspecifico,
                                    MontoImpuestoSelectivoConsumoAdvalorem = i.MontoImpuestoSelectivoConsumoAdvalorem,
                                    OtrosImpuestosAdicionales = i.OtrosImpuestosAdicionales
                                }).ToList()
                        }
                        : null,
                        MontoTotal = factura.Encabezado?.Totales?.MontoTotal,
                        MontoNoFacturable = factura.Encabezado?.Totales?.MontoNoFacturable,
                        MontoPeriodo = factura.Encabezado?.Totales?.MontoPeriodo
                    },

                },
                AnyElement = factura.any_element
            };
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

            XElement elemento = new("RFCE");
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.Name == "TablaFormasPago") continue;
                var valor = prop.GetValue(obj);
                if (valor == null || (valor is string s && s == "#e")) continue;

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
    }
}
