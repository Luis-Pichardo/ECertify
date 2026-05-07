/*********************************************************************
 *                        DESARROLLADOR ENCARGADO:                    *
 *                             Luís Pichardo                         *
 *                                                                   *
 * Código desarrollado y mantenido por Luís Pichardo, quien es el   *
 * responsable principal de esta implementación.                     *
 *********************************************************************/


using System.Xml;
using eCertify.Utils;
using eCertify.Interfaces;
using eCertify.Models;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;

namespace eCertify.Services
{
    public class AprobacionComercialService : IAprobacionComercialService
    {

        private readonly IFileStorageManager _fileStorageManager;
        private readonly ISemillaService _semillaService;
        private readonly ILogger<AprobacionComercialService> _logger;
        private readonly IEmpresaService _empresaService;
        private readonly IWebHostEnvironment _env;

        public AprobacionComercialService(
            IFileStorageManager fileStorageManager,
            ISemillaService semillaService,
            ILogger<AprobacionComercialService> logger,
            IEmpresaService empresaService)
        {
            _fileStorageManager = fileStorageManager;
            _semillaService = semillaService;
            _logger = logger;
            _empresaService = empresaService;
        }

        /// <summary>
        /// Genera y firma un XML de aprobación comercial (ACECF) con el certificado de la empresa según el RNC del comprador,
        /// lo guarda localmente y lo envía al endpoint de la DGII, retornando su respuesta JSON.
        /// </summary>
        /// <param name="modelo">Modelo ACECF con los datos de la aprobación comercial.</param>
        /// <returns>Respuesta JSON de la DGII tras enviar el XML firmado.</returns>
        /// <exception cref="Exception">Se lanza si ocurre un error en el proceso.</exception>
        public async Task<string> GenerarYFirmarXmlAsync(ACECF modelo)
        {
            try
            {
                _logger.LogInformation("Iniciando generación de XML para aprobación comercial");

                string rnc = modelo.DetalleAprobacionComercial?.RNCComprador ?? throw new Exception("RNC del comprador no puede ser nulo");
                string eNCF = modelo.DetalleAprobacionComercial?.eNCF ?? throw new Exception("eNCF no puede ser nulo");
                string fileName = $"{"ACECF"}{rnc}{eNCF}.xml";

                // Serializar el modelo a XML
                var serializer = new XmlSerializer(typeof(ACECF));
                var xmlDoc = new XmlDocument();
                using (var ms = new MemoryStream())
                {
                    serializer.Serialize(ms, modelo);
                    ms.Position = 0;
                    xmlDoc.Load(ms);
                }

                _logger.LogInformation("XML generado correctamente, procediendo a firmar");

                // Obtener empresa para firmar XML
                var empresa = await _empresaService.GetEmpresaByRncAsync(rnc);
                if (empresa == null)
                {
                    _logger.LogError("Empresa con RNC {rnc} no encontrada", rnc);
                    throw new Exception($"Empresa con RNC {rnc} no encontrada");
                }

                var empresaFirmada = _semillaService.FirmarXml(xmlDoc, empresa);

                // Guardar XML firmado
                var xmlFirmadoString = empresaFirmada.OuterXml;
                await _fileStorageManager.SaveXmlAsync(rnc, xmlFirmadoString, fileName, FileStorageManager.StorageType.Aprobaciones);

                string ruta = Path.Combine(
                _fileStorageManager.GetDynamicFolderPath(rnc, FileStorageManager.StorageType.Aprobaciones),
                fileName);

                _logger.LogInformation("XML firmado y guardado correctamente en {ruta}", ruta);

                // === OBTENER TOKEN DE LA DGII ===
                string token = await _semillaService.ObtenerTokenAsync(rnc);
                _logger.LogInformation("Token obtenido exitosamente");

                // === ENVIAR ARCHIVO A DGII ===
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var multipartContent = new MultipartFormDataContent();
                await using var fileStream = new FileStream(ruta, FileMode.Open, FileAccess.Read);
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

                multipartContent.Add(fileContent, "xml", fileName);

                var response = await httpClient.PostAsync(Constants.AprobacionComercialEndpoint, multipartContent);
                string respuestaJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Respuesta DGII: {respuesta}", respuestaJson);

                // retorna el Json de respuesta de la DGII
                return respuestaJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GenerarYFirmarXmlAsync");
                throw;
            }
        }

        //Cuidado con modificar este metodo para validar la aprobacion comercial que envia la DGII, ya que tiene las validaciones especificas que requiere la DGII.
        public (bool Exito, string Mensaje) ValidarAprobacionComercial(string xmlString)
        {
            try
            {
                _logger.LogInformation("Iniciando validación de aprobación comercial.");

                var xmlDoc = new XmlDocument
                {
                    PreserveWhitespace = true
                };
                xmlDoc.LoadXml(xmlString);
                _logger.LogInformation("XML cargado correctamente.");

                var signedXml = new SignedXml(xmlDoc);

                var signatureNode = xmlDoc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#")[0] as XmlElement;
                if (signatureNode == null)
                {
                    _logger.LogWarning("El XML no contiene un nodo de firma digital.");
                    return (false, "El XML no tiene nodo de firma digital.");
                }

                signedXml.LoadXml(signatureNode);
                _logger.LogInformation("Nodo de firma cargado.");

                var certNode = signatureNode.GetElementsByTagName("X509Certificate")[0];
                if (certNode == null)
                {
                    _logger.LogWarning("No se encontró el nodo X509Certificate.");
                    return (false, "No se encontró el certificado dentro del XML.");
                }

                var certString = certNode.InnerText;
                var cert = new X509Certificate2(Convert.FromBase64String(certString));
                _logger.LogInformation("Certificado extraído correctamente.");

                bool firmaValida = signedXml.CheckSignature(cert, true);
                if (!firmaValida)
                {
                    _logger.LogWarning("La firma digital es inválida o el contenido fue alterado.");
                    return (false, "Firma digital inválida o el contenido fue alterado.");
                }

                _logger.LogInformation("Firma digital verificada correctamente.");

                var detalleNode = xmlDoc.SelectSingleNode("//ACECF/DetalleAprobacionComercial");
                if (detalleNode == null)
                {
                    _logger.LogWarning("Falta el nodo DetalleAprobacionComercial.");
                    return (false, "Estructura XML inválida. Falta nodo DetalleAprobacionComercial.");
                }

                var camposRequeridos = new[]
                {
                    "Version", "RNCEmisor", "eNCF", "FechaEmision", "MontoTotal",
                    "RNCComprador", "Estado", "FechaHoraAprobacionComercial"
                };

                foreach (var campo in camposRequeridos)
                {
                    var nodo = detalleNode.SelectSingleNode(campo);
                    if (nodo == null || string.IsNullOrWhiteSpace(nodo.InnerText))
                    {
                        _logger.LogWarning("Campo requerido '{Campo}' no se encuentra o está vacío.", campo);
                        return (false, $"El campo '{campo}' es requerido y no se encuentra o está vacío.");
                    }
                }

                var encf = detalleNode.SelectSingleNode("eNCF")?.InnerText ?? "";
                if (!string.IsNullOrWhiteSpace(encf) && encf.Length >= 3)
                {
                    var tipo = encf.Substring(1, 2);
                    var tiposInvalidos = new[] { "32", "41", "43", "46", "47" };
                    if (tiposInvalidos.Contains(tipo))
                    {
                        _logger.LogWarning("Tipo de eCF no válido para aprobación comercial: {Tipo}", tipo);
                        return (false, $"El tipo de eCF '{tipo}' no aplica para aprobación comercial.");
                    }
                }

                _logger.LogInformation("Validación completada exitosamente. Firma y estructura válidas.");
                return (true, "Firma válida y estructura XML aceptada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en validación de firma.");
                return (false, $"Error en validación de firma: {ex.Message}");
            }
        }


    }
}
