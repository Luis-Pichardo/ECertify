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
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;

namespace eCertify.Services
{
    public class CommercialApprovalService : ICommercialApprovalService
    {
        private readonly IFileStorageManager _fileStorageManager;
        private readonly ISemillaService _semillaService;
        private readonly ILogger<CommercialApprovalService> _logger;
        private readonly IEmpresaService _empresaService;

        public CommercialApprovalService(
            IFileStorageManager fileStorageManager,
            ISemillaService semillaService,
            ILogger<CommercialApprovalService> logger,
            IEmpresaService empresaService)
        {
            _fileStorageManager = fileStorageManager;
            _semillaService = semillaService;
            _logger = logger;
            _empresaService = empresaService;
        }

        public async Task<string> GenerateAndSignXmlAsync(ACECF model)
        {
            try
            {
                _logger.LogInformation("Starting XML generation for commercial approval");

                string rnc      = model.DetalleAprobacionComercial?.RNCComprador ?? throw new Exception("Buyer RNC cannot be null");
                string encf     = model.DetalleAprobacionComercial?.eNCF         ?? throw new Exception("eNCF cannot be null");
                string fileName = $"ACECF{rnc}{encf}.xml";

                var serializer = new XmlSerializer(typeof(ACECF));
                var xmlDoc     = new XmlDocument();
                using (var ms = new MemoryStream())
                {
                    serializer.Serialize(ms, model);
                    ms.Position = 0;
                    xmlDoc.Load(ms);
                }

                _logger.LogInformation("XML generated, proceeding to sign");

                var company = await _empresaService.GetEmpresaByRncAsync(rnc);
                if (company == null)
                {
                    _logger.LogError("Company with RNC {Rnc} not found", rnc);
                    throw new Exception($"Company with RNC {rnc} not found");
                }

                var signedDoc     = _semillaService.FirmarXml(xmlDoc, company);
                var signedXmlStr  = signedDoc.OuterXml;

                await _fileStorageManager.SaveXmlAsync(rnc, signedXmlStr, fileName, FileStorageManager.StorageType.Aprobaciones);

                string filePath = Path.Combine(
                    _fileStorageManager.GetDynamicFolderPath(rnc, FileStorageManager.StorageType.Aprobaciones),
                    fileName);

                _logger.LogInformation("Signed XML saved to {FilePath}", filePath);

                string dgiiToken = await _semillaService.ObtenerTokenAsync(rnc);

                using var httpClient      = new HttpClient();
                using var multipart       = new MultipartFormDataContent();
                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dgiiToken);

                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                multipart.Add(fileContent, "xml", fileName);

                var dgiiResponse = await httpClient.PostAsync(Constants.AprobacionComercialEndpoint, multipart);
                var responseBody = await dgiiResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("DGII response: {Response}", responseBody);
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateAndSignXmlAsync");
                throw;
            }
        }

        // Do NOT modify this validation — it implements DGII-specific requirements for incoming approvals.
        public (bool Success, string Message) ValidateApproval(string xmlContent)
        {
            try
            {
                _logger.LogInformation("Starting commercial approval validation");

                var xmlDoc = new XmlDocument { PreserveWhitespace = true };
                xmlDoc.LoadXml(xmlContent);

                var signedXml = new SignedXml(xmlDoc);
                var signatureNode = xmlDoc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#")[0] as XmlElement;

                if (signatureNode == null)
                {
                    _logger.LogWarning("XML does not contain a digital signature node");
                    return (false, "El XML no tiene nodo de firma digital.");
                }

                signedXml.LoadXml(signatureNode);

                var certNode = signatureNode.GetElementsByTagName("X509Certificate")[0];
                if (certNode == null)
                {
                    _logger.LogWarning("X509Certificate node not found");
                    return (false, "No se encontró el certificado dentro del XML.");
                }

                var cert        = new X509Certificate2(Convert.FromBase64String(certNode.InnerText));
                bool signValid  = signedXml.CheckSignature(cert, true);

                if (!signValid)
                {
                    _logger.LogWarning("Digital signature is invalid or content was tampered");
                    return (false, "Firma digital inválida o el contenido fue alterado.");
                }

                var detailNode = xmlDoc.SelectSingleNode("//ACECF/DetalleAprobacionComercial");
                if (detailNode == null)
                {
                    _logger.LogWarning("Missing DetalleAprobacionComercial node");
                    return (false, "Estructura XML inválida. Falta nodo DetalleAprobacionComercial.");
                }

                var requiredFields = new[]
                {
                    "Version", "RNCEmisor", "eNCF", "FechaEmision", "MontoTotal",
                    "RNCComprador", "Estado", "FechaHoraAprobacionComercial"
                };

                foreach (var field in requiredFields)
                {
                    var node = detailNode.SelectSingleNode(field);
                    if (node == null || string.IsNullOrWhiteSpace(node.InnerText))
                    {
                        _logger.LogWarning("Required field '{Field}' is missing or empty", field);
                        return (false, $"El campo '{field}' es requerido y no se encuentra o está vacío.");
                    }
                }

                var encfValue = detailNode.SelectSingleNode("eNCF")?.InnerText ?? "";
                if (encfValue.Length >= 3)
                {
                    var ecfType = encfValue.Substring(1, 2);
                    var invalidTypes = new[] { "32", "41", "43", "46", "47" };
                    if (invalidTypes.Contains(ecfType))
                    {
                        _logger.LogWarning("Invalid ECF type for commercial approval: {Type}", ecfType);
                        return (false, $"El tipo de eCF '{ecfType}' no aplica para aprobación comercial.");
                    }
                }

                _logger.LogInformation("Validation completed successfully");
                return (true, "Firma válida y estructura XML aceptada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during approval validation");
                return (false, $"Error en validación de firma: {ex.Message}");
            }
        }
    }
}
