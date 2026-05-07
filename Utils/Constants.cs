using Microsoft.AspNetCore.Http;
using static System.Net.WebRequestMethods;

namespace eCertify.Utils
{
    public class Constants
    {
        public const string UrlDgii = "CerteCF";
        public const string AutenticarSemillaEndpoint = "https://ecf.dgii.gov.do/" + UrlDgii + "/Autenticacion/api/Autenticacion/Semilla";
        public const string ValidarSemillaEndpoint = "https://ecf.dgii.gov.do/" + UrlDgii + "/Autenticacion/api/Autenticacion/ValidarSemilla";
        public const  string FacturasElectronicasEndpoint = "https://ecf.dgii.gov.do/" + UrlDgii + "/Recepcion/api/FacturasElectronicas";
        public const string ConsultarFacturasEndpoint = "https://ecf.dgii.gov.do/" + UrlDgii + "/ConsultaResultado/api/Consultas/Estado";
        public const string AprobacionComercialEndpoint = "https://ecf.dgii.gov.do/" + UrlDgii + "/AprobacionComercial/api/AprobacionComercial";
        public const string URLQR = "https://ecf.dgii.gov.do/" + UrlDgii + "/ConsultaTimbre";
        public const string XmlSavePath = "Storage/Archivos/semilla.xml";
        public const string SignedXmlPath = "Storage/Archivos/semilla_firmado.xml";
        public const string CertificatePath = "Storage/Certificados/20250210-1801071-GNCL6CWET.p12";
        public const string certificatePassword = "Via8098601695";
        public const string rncEmisor = "132650761";
        public const string UrlSinEndpoint = "https://ecf.dgii.gov.do/" + UrlDgii;
        //url para los resumenes de facturas
        public const string UrlRFCERecepcion = "https://ecf.dgii.gov.do/" + UrlDgii + "/recepcionfc/help/index.html";


        //Exclusivos para produccion CerteCF
        public const string UrlProd = "CerteCF";
        public const string EnviarFacturasProduccion = "https://ecf.dgii.gov.do/" + UrlProd + "/recepcion/api/FacturasElectronicas";
        public const string EnviarResumenProduccion = "https://fc.dgii.gov.do/" + UrlProd + "/recepcionfc/api/recepcion/ecf";
        public const string ConsultarFacturasProduccion = "https://ecf.dgii.gov.do/" + UrlProd + "/consultaresultado/api/Consultas/Estado";
        public const string ConsultarResumenProduccion = "https://fc.dgii.gov.do/" + UrlProd + "/consultarfce/api/Consultas/Consultas";
        public const string ConsultaTiembreProduccion = "https://ecf.dgii.gov.do/" + UrlProd + "/consultatimbre?";
    }
}