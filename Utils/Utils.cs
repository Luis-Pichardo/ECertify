using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Globalization;

namespace eCertify.Utils
{
    //clases para metodos estaticos, los metodos deben cumplir con (SRP). Principio de responsabilidad unica.
    public static class Utils
    {
        public static string LimpiarRNC(string rnc)
        {
            // Elimina caracteres no numéricos (excepto guiones si son necesarios)
            return new string(rnc.Where(c => char.IsDigit(c)).ToArray());
        }

        public static string ExtractTokenFromJson(string jsonResponse)
        {
            try
            {
                // Parsear la respuesta JSON utilizando System.Text.Json
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    // Suponiendo que el token está en la propiedad "token"
                    return doc.RootElement.GetProperty("token").GetString();
                }
            }
            catch (Exception ex)
            {
                // Manejar errores
                Console.WriteLine("Error al extraer el token: " + ex.Message);
                return null;
            }
        }

        public static string ExtractTrackIdFromJson(string jsonResponse)
        {
            try
            {
                var jsonObject = System.Text.Json.JsonDocument.Parse(jsonResponse).RootElement;

                if (jsonObject.TryGetProperty("trackId", out var trackIdProperty))
                {
                    return trackIdProperty.GetString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al extraer el trackid: " + ex.Message);
            }

            return null;
        }


        public static XmlDocument FirmarXml(XmlDocument xmlDoc, string pathCert, string passCert, bool addDate = true)
        {
            try
            {
                // Verificar que el archivo del certificado exista
                if (!System.IO.File.Exists(pathCert))
                {
                    // _logger.LogError("❌ Certificado no encontrado en: {Path}", pathCert);
                    throw new FileNotFoundException("El archivo de certificado no existe.", pathCert);
                }

                // Cargar el certificado con flags adecuados para producción (IIS)
                var cert = new X509Certificate2(
                    pathCert,
                    passCert,
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.Exportable |
                    X509KeyStorageFlags.PersistKeySet
                );

                // Extraer clave privada
                var rsaPrivateKey = cert.GetRSAPrivateKey();
                if (rsaPrivateKey == null)
                {
                    //_logger.LogError("❌ No se pudo obtener la clave privada del certificado.");
                    throw new CryptographicException("Clave privada no disponible en el certificado.");
                }

                // Preparar para firmar
                var signedXml = new SignedXml(xmlDoc)
                {
                    SigningKey = rsaPrivateKey
                };
                signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                var reference = new Reference
                {
                    Uri = "",
                    DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256"
                };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                var keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;

                // Firmar el XML
                signedXml.ComputeSignature();
                XmlElement xmlSignature = signedXml.GetXml();

                xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlSignature, true));

                if (addDate)
                {
                    var fechaFirma = xmlDoc.CreateElement("FechaHoraFirma");
                    fechaFirma.InnerText = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    xmlDoc.DocumentElement.AppendChild(fechaFirma);
                }
                return xmlDoc;
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "❌ Error durante el proceso de firma.");
                throw; // Se vuelve a lanzar para que el controlador lo capture
            }
        }

        //Esta funcion reemplaza caracteres especiales en una cadena para que sea compatible con códigos QR.
        public static string ReemplazarCaracteresQR(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return input
                .Replace(" ", "%20")
                .Replace("!", "%21")
                .Replace("#", "%23")
                .Replace("$", "%24")
                .Replace("&", "%26")
                .Replace("'", "%27")
                .Replace("(", "%28")
                .Replace(")", "%29")
                .Replace("*", "%2A")
                .Replace("+", "%2B")
                .Replace(",", "%2C")
                .Replace("/", "%2F")
                .Replace(":", "%3A")
                .Replace(";", "%3B")
                .Replace("=", "%3D")
                .Replace("?", "%3F")
                .Replace("@", "%40")
                .Replace("[", "%5B")
                .Replace("]", "%5D")
                .Replace("\"", "%22")
                .Replace("-", "%2D")
                .Replace(".", "%2E")
                .Replace("<", "%3C")
                .Replace(">", "%3E")
                .Replace("\\", "%5C")
                .Replace("^", "%5E")
                .Replace("_", "%5F")
                .Replace("`", "%60");
        }

        public static decimal ParseDecimalOrDefault(string input, decimal defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            input = input.Replace(",", ".");

            if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            return defaultValue;
        }

    }
}
