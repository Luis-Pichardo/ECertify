using System.Collections.Generic;
using System.Xml.Serialization;

namespace eCertify.Models.ResumenesModel
{
    [XmlRoot("RFCE")]
    public class ResumenesModel
    {
        public Encabezado? Encabezado { get; set; }
        public string? AnyElement { get; set; }
    }

    public class Encabezado
    {
        public string? Version { get; set; }
        public IdDoc? IdDoc { get; set; }
        public Emisor? Emisor { get; set; }
        public Comprador? Comprador { get; set; }
        public Totales? Totales { get; set; }
        public string? CodigoSeguridadeCF { get; set; }
    }

    public class IdDoc
    {
        public string? TipoeCF { get; set; }
        public string? eNCF { get; set; }
        public string? TipoIngresos { get; set; }
        public string? TipoPago { get; set; }
        [XmlElement("FormaDePago", IsNullable = true)]
        public List<FormaDePago> TablaFormasPago { get; set; }
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
        public string? FechaEmision { get; set; }
    }

    public class Comprador
    {
        public string? RNCComprador { get; set; }
        public string? IdentificadorExtranjero { get; set; }
        public string? RazonSocialComprador { get; set; }
    }

    public class Totales
    {
        public string? MontoGravadoTotal { get; set; }
        public string? MontoGravadoI1 { get; set; }
        public string? MontoGravadoI2 { get; set; }
        public string? MontoGravadoI3 { get; set; }
        public string? MontoExento { get; set; }
        public string? TotalITBIS { get; set; }
        public string? TotalITBIS1 { get; set; }
        public string? TotalITBIS2 { get; set; }
        public string? TotalITBIS3 { get; set; }
        public string? MontoImpuestoAdicional { get; set; }
        public ImpuestosAdicionales? ImpuestosAdicionales { get; set; }
        public string? MontoTotal { get; set; }
        public string? MontoNoFacturable { get; set; }
        public string? MontoPeriodo { get; set; }
    }

    public class ImpuestosAdicionales
    {
        public List<ImpuestoAdicional>? ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicional
    {
        public string? TipoImpuesto { get; set; }
        public string? MontoImpuestoSelectivoConsumoEspecifico { get; set; }
        public string? MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
        public string? OtrosImpuestosAdicionales { get; set; }
    }
}
