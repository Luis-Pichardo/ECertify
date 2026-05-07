using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml;
using Newtonsoft.Json;
using eCertify.Interfaces;
using eCertify.Utils;

namespace eCertify.Services
{
    public class FacturasElectronicasService : IFacturasElectronicasService
    {
        private readonly HttpClient _httpClient;
        public FacturasElectronicasService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        //para enviar las facturas ya firmadas al endpoint de la dgii
        public async Task<string> EnviarFactura(XmlDocument xmlDocument, string fileName, string bearerToken)
        {
            //string endpoint = "https://ecf.dgii.gov.do/CerteCF/Recepcion/api/FacturasElectronicas";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    MultipartFormDataContent form = new MultipartFormDataContent();
                    byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(xmlDocument.OuterXml);
                    form.Add(new ByteArrayContent(fileBytes), "xml", fileName);

                    HttpResponseMessage response = await client.PostAsync(Utils.Constants.FacturasElectronicasEndpoint, form);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error en {Utils.Constants.FacturasElectronicasEndpoint}: {response.StatusCode} - {errorMessage}");
                    }

                    string responseContent = await response.Content.ReadAsStringAsync();
                    return Utils.Utils.ExtractTrackIdFromJson(responseContent); //ExtractTrackIdFromJson(responseContent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en EnviarFactura: {ex.Message}");
            }
        }
        //consultar las informaciones de la factura en la pagina de la dgii
        public async Task<ApiResponse> ConsultarFacturaEnviada(string trackId, string bearerToken)
        {
            var url = $"{Constants.ConsultarFacturasEndpoint}?TrackId={trackId}";

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error en {url}: {response.StatusCode} - {errorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en ConsultarFacturaEnviada: {ex.Message}");
            }
        }
        public class ApiResponse
        {
            public string TrackId { get; set; }
            public string Codigo { get; set; }
            public string Estado { get; set; }
            public string Rnc { get; set; }
            public string Encf { get; set; }
            public bool SecuenciaUtilizada { get; set; }
            public string FechaRecepcion { get; set; }
            public Mensaje[] Mensajes { get; set; }
        }

        public class Mensaje
        {
            public string Valor { get; set; }
            public int Codigo { get; set; }
        }
    }
}
