using System;
using System.Xml.Serialization;

namespace eCertify.Models
{
    [XmlRoot("ACECF")]
    public class ACECF
    {
        public DetalleAprobacionComercial DetalleAprobacionComercial { get; set; }
    }

    public class DetalleAprobacionComercial
    {
        public string Version { get; set; }
        public string RNCEmisor { get; set; }
        public string eNCF { get; set; }
        public string FechaEmision { get; set; }
        public decimal MontoTotal { get; set; }
        public string RNCComprador { get; set; }
        public int Estado { get; set; }
        public string? DetalleMotivoRechazo { get; set; }
        public string FechaHoraAprobacionComercial { get; set; }
    }
}
