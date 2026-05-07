using System.Text.Json.Serialization;

namespace eCertify.DTOs.Front
{
    public class SimulacionECFRequestDTO
    {
        [JsonPropertyName("ECF")]
        public ECF Ecf { get; set; }
        public class ECF
        {
            public Encabezado Encabezado { get; set; }
            public List<Item> DetallesItems { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public InformacionReferencia InformacionReferencia { get; set; }
        }

        public class Encabezado
        {
            public string Version { get; set; }
            public IdDoc IdDoc { get; set; }
            public Emisor Emisor { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Comprador Comprador { get; set; }
            public Totales Totales { get; set; }
        }

        public class IdDoc
        {
            public string TipoeCF { get; set; }
            public string eNCF { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string FechaVencimientoSecuencia { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string indicadorNotaCredito { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string indicadorMontoGravado { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string TipoIngresos { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string TipoPago { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<FormaDePago> TablaFormasPago { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string numeroCuentaPago { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string bancoPago { get; set; }
        }

        public class FormaDePago
        {
            public string FormaPago { get; set; }
            public string MontoPago { get; set; }
        }

        public class Emisor
        {
            public string RNCEmisor { get; set; }
            public string RazonSocialEmisor { get; set; }
            public string NombreComercial { get; set; }
            public string DireccionEmisor { get; set; }
            public string CorreoEmisor { get; set; }
            public string FechaEmision { get; set; }
        }

        public class Comprador
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string RNCComprador { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string identificadorExtranjero { get; set; }
            public string RazonSocialComprador { get; set; }
        }

        public class Totales
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string montoGravadoTotal { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string montoGravadoI1 { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string montoGravadoI3 { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string itbiS1 { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string itbiS3 { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string totalITBIS { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string totalITBIS1 { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string totalITBIS3 { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string MontoExento { get; set; }

            public string MontoTotal { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string valorPagar { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string totalITBISRetenido { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string totalISRRetencion { get; set; }
        }

        public class Item
        {
            public string NumeroLinea { get; set; }
            public string IndicadorFacturacion { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Retencion retencion { get; set; }
            public string NombreItem { get; set; }
            public string IndicadorBienoServicio { get; set; }
            public string CantidadItem { get; set; }
            public string UnidadMedida { get; set; }
            public string PrecioUnitarioItem { get; set; }
            public string MontoItem { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string ITBISItem { get; set; }
        }

        public class Retencion
        {
            public string indicadorAgenteRetencionoPercepcion { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string montoITBISRetenido { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string montoISRRetenido { get; set; }
        }

        public class InformacionReferencia
        {
            public string NCFModificado { get; set; }
            public string FechaNCFModificado { get; set; }
            public string CodigoModificacion { get; set; }
            public string RazonModificacion { get; set; }
        }

    }
}
