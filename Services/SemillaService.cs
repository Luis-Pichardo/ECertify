
using eCertify.Utils;
using eCertify.Interfaces;
using System.Xml;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using eCertify.Data;
using Microsoft.EntityFrameworkCore;

namespace eCertify.Services

{
    public class SemillaService : ISemillaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SemillaService> _logger;
        private readonly SogeDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Eliminamos los parámetros del constructor y usamos directamente las constantes
        public SemillaService(HttpClient httpClient, ILogger<SemillaService> logger, SogeDbContext context, IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _logger = logger;
            _context = context;

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("SemillaService inicializado");
            _env = env;
        }

        public async Task<string> ObtenerTokenAsync(string rnc)
        {
            _logger.LogInformation("Iniciando proceso de token para RNC: {rnc}", rnc);

            try
            {
                var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.RNC == rnc);

                if (empresa == null)
                {
                    _logger.LogError("Empresa con RNC {rnc} no encontrada", rnc);
                    throw new Exception($"Empresa con RNC {rnc} no encontrada");
                }

                if (string.IsNullOrWhiteSpace(empresa.NombreCertificadop12) || string.IsNullOrWhiteSpace(empresa.CertificadoPass))
                {
                    _logger.LogError("Certificado o contraseña no definidos para la empresa con RNC {rnc}", rnc);
                    throw new Exception("Certificado o contraseña no definidos en la empresa");
                }

                var xmlDoc = new XmlDocument();
                var xmlString = await _httpClient.GetStringAsync(Constants.AutenticarSemillaEndpoint);
                xmlDoc.LoadXml(xmlString);

                var xmlFirmado = FirmarXml(xmlDoc, empresa);

                return await EnviarXmlFirmado(xmlFirmado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener token");
                throw;
            }
        }

        public XmlDocument FirmarXml(XmlDocument xmlDoc, Models.Empresa empresa, bool agregarFechaHora = false)
        {
            _logger.LogInformation("Firmando XML para empresa con RNC: {rnc}", empresa.RNC);

            try
            {
                if (agregarFechaHora)
                {
                    var fechaHoraElement = xmlDoc.CreateElement("FechaHoraFirma");
                    fechaHoraElement.InnerText = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                    xmlDoc.DocumentElement?.AppendChild(fechaHoraElement);
                    _logger.LogDebug("Fecha y hora agregadas al XML");
                }

                var rutaCertificado = Path.Combine(_env.ContentRootPath, "Storage", "Certificados", empresa.RNC!, empresa.NombreCertificadop12!);
                var password = empresa.CertificadoPass!;

                using var cert = new X509Certificate2(
                    rutaCertificado,
                    password,
                    X509KeyStorageFlags.Exportable);

                using var rsa = cert.GetRSAPrivateKey() ?? throw new Exception("No se pudo obtener la clave privada");

                var signedXml = new SignedXml(xmlDoc) { SigningKey = rsa };
                signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                var reference = new Reference { Uri = "", DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256" };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                signedXml.KeyInfo = new KeyInfo();
                signedXml.KeyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.ComputeSignature();

                xmlDoc.DocumentElement?.AppendChild(xmlDoc.ImportNode(signedXml.GetXml(), true));

                _logger.LogInformation("XML firmado correctamente");
                return xmlDoc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al firmar XML");
                throw new Exception("Error al firmar XML", ex);
            }
        }

        public async Task<string> EnviarXmlFirmado(XmlDocument xmlDoc)
        {
            _logger.LogInformation("Enviando XML firmado...");

            try
            {
                using var form = new MultipartFormDataContent();
                using var xmlStream = new MemoryStream();

                xmlDoc.Save(xmlStream);
                xmlStream.Position = 0;

                form.Add(new StreamContent(xmlStream), "xml", "semilla_firmado.xml");
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.PostAsync(Constants.ValidarSemillaEndpoint, form);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error al enviar XML. Código: {StatusCode}, Respuesta: {ErrorBody}",
                        response.StatusCode, errorBody);
                    throw new HttpRequestException($"Error al enviar XML: {response.StatusCode}, {errorBody}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var token = Utils.Utils.ExtractTokenFromJson(jsonResponse);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token no encontrado en la respuesta");
                    throw new Exception("Token no encontrado en la respuesta");
                }

                _logger.LogInformation("Token obtenido correctamente");
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar XML firmado");
                throw;
            }
        }
    }
}
