using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Xml.Linq;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using QRCoder;
using eCertify.Models;
using eCertify.Services;
using eCertify.Utils;
using eCertify.Interfaces;
using System.Text.Json;
using eCertify.DTOs;
using Microsoft.AspNetCore.Authorization;


namespace eCertify.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RepresentacionImpresaController : ControllerBase
    {
        private readonly QuerySqlService _querySqlService;
        private readonly IWebHostEnvironment _env;
        private readonly IFileStorageManager _fileStorageManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RepresentacionImpresaController> _logger;
        public RepresentacionImpresaController(QuerySqlService querySqlService, IWebHostEnvironment env, IFileStorageManager fileStorageManager, IHttpClientFactory httpClientFactory, ILogger<RepresentacionImpresaController> logger)
        {
            _querySqlService = querySqlService;
            _env = env;
            _fileStorageManager = fileStorageManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        //ESTA PENDIENTE LO DE LOS CAMPOS QUE SON OBLIGATORIOS PARA EL PLENO FUNCIONAMIENTO DE ESTE ENDPOINT 
        //endpoint actualmente usandose pa E31,E31,E33,E34
        [HttpPost("generar-pdf")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GenerarPdf([FromForm] UploadXmlRequest request)
        {
            var archivoXml = request.ArchivoXml;
            if (archivoXml == null || archivoXml.Length == 0)
                return BadRequest("Debe cargar un archivo XML válido.");

            try
            {
                string xmlContent;
                using (var reader = new StreamReader(archivoXml.OpenReadStream()))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }

                var doc = XDocument.Parse(xmlContent);
                var idDoc = doc.Descendants("IdDoc").FirstOrDefault();
                var emisor = doc.Descendants("Emisor").FirstOrDefault();
                var comprador = doc.Descendants("Comprador").FirstOrDefault();
                var totales = doc.Descendants("Totales").FirstOrDefault();
                var InformacionReferencia = doc.Descendants("InformacionReferencia").FirstOrDefault();
                var DescuentoORecargo = doc.Descendants("DescuentoORecargo").FirstOrDefault();
                var items = doc.Descendants("Item").ToList();

                var firma = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "SignatureValue")?.Value ?? "";

                var fechaFirma = doc.Descendants("FechaHoraFirma").FirstOrDefault()?.Value ?? "";
                var eNCF = idDoc?.Element("eNCF")?.Value ?? "";

                var NCFModificado = InformacionReferencia?.Element("NCFModificado")?.Value ?? "";
                var CodigoMoficacion = InformacionReferencia?.Element("CodigoModificacion")?.Value ?? "0";
                var RazonModificacion = InformacionReferencia?.Element("RazonModificacion")?.Value ?? "";
                var fechaEmision = emisor?.Element("FechaEmision")?.Value ?? "";
                var rncEmisor = emisor?.Element("RNCEmisor")?.Value ?? "SINRNC";


                //descuentos o recargo 
                var MontoDescuentooRecargo = DescuentoORecargo?.Element("MontoDescuentooRecargo")?.Value ?? "0";
                // Extraer posibles valores del comprador
                string rncComprador = comprador?.Element("RNCComprador")?.Value;
                string idExtranjero = comprador?.Element("IdentificadorExtranjero")?.Value;

                // Elegir el valor correcto
                string valorrncComprador = !string.IsNullOrWhiteSpace(rncComprador) ? rncComprador : !string.IsNullOrWhiteSpace(idExtranjero) ? idExtranjero : null;

                // Generar QR

                string urlQR = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?" +
                $"RncEmisor={Utils.Utils.ReemplazarCaracteresQR(rncEmisor)}" +
                (!string.IsNullOrEmpty(valorrncComprador) ? $"&RncComprador={Utils.Utils.ReemplazarCaracteresQR(valorrncComprador)}" : "") +
                $"&ENCF={Utils.Utils.ReemplazarCaracteresQR(eNCF)}" +
                $"&FechaEmision={Utils.Utils.ReemplazarCaracteresQR(fechaEmision)}" +
                $"&MontoTotal={Utils.Utils.ReemplazarCaracteresQR(totales?.Element("MontoTotal")?.Value ?? "")}" +
                $"&FechaFirma={Utils.Utils.ReemplazarCaracteresQR(fechaFirma)}" +
                $"&CodigoSeguridad={Utils.Utils.ReemplazarCaracteresQR(firma.Substring(0, 6))}";


                // string urlQR = $"https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?" +
                //$"RncEmisor={rncEmisor}" +
                //(!string.IsNullOrEmpty(valorrncComprador) ? $"&RncComprador={valorrncComprador}" : "") +
                //$"&ENCF={eNCF}" +
                //$"&FechaEmision={fechaEmision}" +
                //$"&MontoTotal={totales?.Element("MontoTotal")?.Value}" +
                //$"&FechaFirma={fechaFirma.Replace(" ", "+")}" +
                //$"&CodigoSeguridad={firma.Substring(0, 6)}";

                PdfDocument pdf = new PdfDocument();
                PdfPage page = pdf.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Arial", 12, XFontStyle.Regular);
                XFont font10 = new XFont("Arial", 10, XFontStyle.Regular);
                XFont boldFont = new XFont("Arial", 12, XFontStyle.Bold);
                XFont boldFont11 = new XFont("Arial", 11, XFontStyle.Bold);
                XPen bluePen = new XPen(XColors.Blue, 2);

                int yPoint = 0;
                int leftMargin = 10;
                int rightMargin = 10;
                double usableWidth = page.Width - leftMargin - rightMargin;
                double anchoMaxDireccion = usableWidth * 0.6;

                // Encabezado
                yPoint += 20;

                

                gfx.DrawString($"{emisor.Element("RazonSocialEmisor")?.Value}", boldFont, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString($"RNC: {rncEmisor}", font, XBrushes.Black, new XRect(leftMargin, yPoint + 20, usableWidth, 20), XStringFormats.TopLeft);
                //gfx.DrawString($"Dirección: {emisor.Element("DireccionEmisor")?.Value}", font10, XBrushes.Black, new XRect(leftMargin, yPoint + 40, usableWidth, 20), XStringFormats.TopLeft);
                //gfx.DrawString($"Fecha emision: {emisor.Element("FechaEmision")?.Value}", font, XBrushes.Black, new XRect(leftMargin, yPoint + 60, usableWidth, 20), XStringFormats.TopLeft);
                // 2. Dirección con formato controlado
                string direccion = $"Dirección: {emisor.Element("DireccionEmisor")?.Value}";
                var direccionLines = SplitTextByLength(direccion, 65); // 35 caracteres por línea
                for (int i = 0; i < direccionLines.Length; i++)
                {
                    gfx.DrawString(direccionLines[i], font10, XBrushes.Black,
                        new XRect(leftMargin, yPoint + 40 + (i * 15), usableWidth * 0.6, 15),
                        XStringFormats.TopLeft);
                }

                // 3. Fecha de emisión (se ajusta según líneas de dirección)
                int fechaYPosition = yPoint + 40 + (direccionLines.Length * 15);
                gfx.DrawString($"Fecha emision: {emisor.Element("FechaEmision")?.Value}", font, XBrushes.Black,
                    new XRect(leftMargin, fechaYPosition, usableWidth * 0.6, 20), XStringFormats.TopLeft);


                //saco el nombre del comprobante 
                // Nombre del comprobante
                string NombreNCF = GetNombreComprobante(Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0"));
                gfx.DrawString(NombreNCF, boldFont, XBrushes.Blue, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopRight);
                yPoint += 25; // ← aumentamos la altura

                // eNCF
                TituloConDescri(gfx, "eNCF: ", eNCF, boldFont, font, leftMargin, yPoint, usableWidth);
                yPoint += 20;

                // NCF modificado
                if (!string.IsNullOrEmpty(NCFModificado.ToString()))
                {
                    TituloConDescri(gfx, "NCF Modificado: ", NCFModificado, boldFont, font, leftMargin, yPoint, usableWidth);
                    yPoint += 20;
                }

                // Fecha vencimiento
                string FechaVencimientoNCF = idDoc.Element("FechaVencimientoSecuencia")?.Value ?? "";
                if (!string.IsNullOrEmpty(FechaVencimientoNCF) && Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") != 34)
                {
                    TituloConDescri(gfx, "Fecha Vencimiento: ", FechaVencimientoNCF, boldFont, font, leftMargin, yPoint, usableWidth);
                    yPoint += 20;
                }

                // Razón de modificación
                if (!string.IsNullOrEmpty(RazonModificacion))
                {
                    gfx.DrawString(RazonModificacion, boldFont, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopRight);
                    yPoint += 5;
                }
                else
                {
                    int tipo = Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0");
                    if (tipo == 33 || tipo == 34)
                    {
                        gfx.DrawString(GetNombreCodigoMoficacionNCF(Int32.Parse(CodigoMoficacion.ToString())), boldFont, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopRight);
                        yPoint += 10;
                    }
                }


                yPoint += 50; // espacio para ambos bloques

                //yPoint += 30;

                // Línea azul arriba
                gfx.DrawLine(bluePen, leftMargin, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 5;

                // Comprador
                //lo muestro solo si no es gasto menor
                if (Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") != 43)
                {
                    gfx.DrawString($"Razon social cliente: {comprador.Element("RazonSocialComprador")?.Value}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                    yPoint += 20;
                    gfx.DrawString($"RNC Cliente: {valorrncComprador}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                    yPoint += 20;

                    // Línea azul abajo
                    gfx.DrawLine(bluePen, leftMargin, yPoint, page.Width - rightMargin, yPoint);
                    yPoint += 25;
                }

                // Tabla detalle encabezado
                int colX_Cantidad = leftMargin;
                int colX_Exento = leftMargin + 55;
                int colX_Descripcion = colX_Cantidad + 70;
                int colX_Unidad = colX_Descripcion + 200;
                int colX_Precio = colX_Unidad + 70; //70
                int colX_ITBIS = colX_Precio + 70; //50 cambiar para el E46 de la prueba
                int colX_Total = colX_ITBIS + 70; //90

                gfx.DrawString("Cantidad", boldFont, XBrushes.Black, new XRect(colX_Cantidad, yPoint, 50, 20), XStringFormats.TopLeft);
                //gfx.DrawString("", boldFont, XBrushes.Black, new XRect(colX_Cantidad, yPoint, 60, 20), XStringFormats.TopLeft);
                gfx.DrawString("Descripción", boldFont, XBrushes.Black, new XRect(colX_Descripcion, yPoint, 200, 20), XStringFormats.TopLeft);
                gfx.DrawString("Unidad", boldFont, XBrushes.Black, new XRect(colX_Unidad, yPoint, 80, 10), XStringFormats.TopLeft);
                gfx.DrawString("de medida", boldFont, XBrushes.Black, new XRect(colX_Unidad, yPoint + 10, 80, 10), XStringFormats.TopLeft);

                gfx.DrawString("Precio", boldFont, XBrushes.Black, new XRect(colX_Precio, yPoint, 80, 20), XStringFormats.TopRight);
                gfx.DrawString("ITBIS", boldFont, XBrushes.Black, new XRect(colX_ITBIS, yPoint, 80, 20), XStringFormats.TopRight);
                gfx.DrawString("Valor", boldFont, XBrushes.Black, new XRect(colX_Total, yPoint, 80, 20), XStringFormats.TopRight);
                yPoint += 25;
                gfx.DrawLine(XPens.Black, colX_Cantidad, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 5;
                decimal TotalExento = 0;


                // Altura de cada línea del detalle
                int altoLinea = 20;
                // Margen inferior mínimo para que se vaya a la siguiente pagina 
                double margenInferior = 30;

                foreach (var item in items)
                {
                    // Verifica si queda espacio para otra línea
                    if (yPoint + altoLinea > page.Height - margenInferior)
                    {
                        // Crear nueva página
                        page = pdf.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        gfx = XGraphics.FromPdfPage(page);

                        yPoint = 40; // Reiniciar desde la parte superior (sin encabezado si no lo deseas)
                    }

                    var cantidad = item.Element("CantidadItem")?.Value ?? "";
                    var nombre = item.Element("NombreItem")?.Value ?? "";
                    var unidad = item.Element("UnidadMedida")?.Value ?? "";
                    var precioUnitario = item.Element("PrecioUnitarioItem")?.Value ?? "0";
                    var montoItem = item.Element("MontoItem")?.Value ?? "0";
                    var IndicadorFacturacion = item.Element("IndicadorFacturacion")?.Value ?? "0"; //elIndicador de facturacion es el tipo de itbis(0= no facturable, 4 = Exento)
                    decimal PorcientoItbis = IndicadorFacturacion switch
                    {
                        "1" => 18m,
                        "2" => 16m,
                        "3" => 0m,
                        _ => 0m // para valores como "4", "0", null, etc.
                    };

                    //si es un producto exento
                    string muestraE = "";
                    if (Int32.Parse(IndicadorFacturacion) == 4)
                    {
                        TotalExento += decimal.Parse(cantidad) * decimal.Parse(precioUnitario);
                        muestraE = "E";
                    }

                    decimal Precio = decimal.Parse(precioUnitario);
                    decimal itbis = Precio * (PorcientoItbis / 100);

                    string UnidadDescripcion = _querySqlService.ObtenerDescriUnidad(unidad.ToString());

                    gfx.DrawString(cantidad, font, XBrushes.Black, new XRect(colX_Cantidad, yPoint, 50, 20), XStringFormats.TopLeft);
                    if(muestraE != "")
                    {
                        gfx.DrawString(muestraE, boldFont11, XBrushes.Black, new XRect(colX_Exento, yPoint, 170, 20), XStringFormats.TopLeft);
                    }
                 
                    gfx.DrawString(nombre, font, XBrushes.Black, new XRect(colX_Descripcion, yPoint, 200, 20), XStringFormats.TopLeft);
                    gfx.DrawString(UnidadDescripcion, font, XBrushes.Black, new XRect(colX_Unidad, yPoint, 60, 20), XStringFormats.TopLeft);
                    gfx.DrawString(Convert.ToDecimal(precioUnitario).ToString("N2"), font, XBrushes.Black, new XRect(colX_Precio, yPoint, 80, 20), XStringFormats.TopRight);
                    gfx.DrawString(itbis.ToString("N2"), font, XBrushes.Black, new XRect(colX_ITBIS, yPoint, 80, 20), XStringFormats.TopRight);
                    gfx.DrawString(Convert.ToDecimal(montoItem).ToString("N2"), font, XBrushes.Black, new XRect(colX_Total, yPoint, 80, 20), XStringFormats.TopRight);
                    yPoint += altoLinea;
                }

                //linea debajo del detalle
                gfx.DrawLine(XPens.Black, colX_Cantidad, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 5;
                // Base para totales
                int baseY = yPoint + 20;

                int sumalto = 0;
                var montoGravadoStr = totales.Element("MontoGravadoTotal")?.Value;
                if ((decimal.TryParse(montoGravadoStr, out decimal montoGravado) && montoGravado > 0) || TotalExento == 0)
                {
                    TituloConDescri(gfx, "Subtotal gravado: ", montoGravado.ToString("N2"), boldFont, font, rightMargin, yPoint + 20, usableWidth - 18);

                    sumalto = TotalExento > 0 ? 20 : 0 ; //para si esta el totalexento que deje un espacio 
                }

              
                //TituloConDescri(gfx, "Subtotal gravado: ",Convert.ToDecimal(totales.Element("MontoGravadoTotal")?.Value ?? "0").ToString("N2"), boldFont, font, rightMargin, yPoint + 20, usableWidth -18);
                //sumalto = 20;

                if (TotalExento > 0)
                {
                    //yPoint += 20; // sisguiente linea
                    TituloConDescri(gfx, "Subtotal exento: ", TotalExento.ToString("N2"), boldFont, font, rightMargin, yPoint + 20 + sumalto, usableWidth - 18);
                    sumalto = Convert.ToDecimal(MontoDescuentooRecargo) > 0 ? 20 : 0 ;
                }

                if (Convert.ToDecimal(MontoDescuentooRecargo) > 0)
                {
                  
                    TituloConDescri(gfx, "Total Descuento: ", Convert.ToDecimal(MontoDescuentooRecargo).ToString("N2"), boldFont, font, rightMargin, yPoint + 20 + sumalto, usableWidth - 18);
                }

                TituloConDescri(gfx, "Total ITBIS: ", Convert.ToDecimal(totales.Element("TotalITBIS")?.Value ?? "0").ToString("N2"), boldFont, font, rightMargin, yPoint + 40 + sumalto, usableWidth - 18 );
                TituloConDescri(gfx, "Monto Total: ", Convert.ToDecimal(totales.Element("MontoTotal")?.Value ?? "0").ToString("N2"), boldFont, font, rightMargin, yPoint + 60 + sumalto, usableWidth - 18);

                yPoint += 20;

                // QR
                var qrGenerator = new QRCodeGenerator();
                var qrData = qrGenerator.CreateQrCode(urlQR, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.BitmapByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(20);

                using (var qrStream = new MemoryStream(qrBytes))
                {
                    XImage qrImage = XImage.FromStream(() => new MemoryStream(qrBytes));
                    gfx.DrawImage(qrImage, 20, yPoint + 40, 130, 130);
                }

                yPoint += 170;
                gfx.DrawString($"Codigo de seguridad: {firma.Substring(0, 6)}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                yPoint += 20;
                gfx.DrawString($"Fecha firma: {fechaFirma}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);


                //Por este...
                try
                {
                    // Limpia el RNC de caracteres no válidos
                    string rncLimpio = Utils.Utils.LimpiarRNC(rncEmisor);
                    string fileName = $"{rncLimpio}{eNCF}.pdf";
                    string dynamicFolderPath = _fileStorageManager.GetDynamicFolderPath(
                        rncLimpio,
                        FileStorageManager.StorageType.RIPdfs
                    );
                    string fullPath = Path.Combine(dynamicFolderPath, fileName);
                    pdf.Save(fullPath);

                    string pdfUrl = $"{Request.Scheme}://{Request.Host}/Storage/RIPdfs/{rncLimpio}/{fileName}";
                    // Devuelve el archivo
                    //byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                    //return File(fileBytes, "application/pdf", fileName), QrUrl = urlQR;

                    return new JsonResult(new
                    {
                        PdfUrl = pdfUrl,
                        QrUrl = urlQR
                    });


                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error al guardar el PDF: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generando PDF: {ex.Message}");
            }
        }

        [HttpPost("generar-pdf-E32")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GenerarPdfE32([FromForm] UploadXmlRequest request)
        {
            var archivoXml = request.ArchivoXml;
            if (archivoXml == null || archivoXml.Length == 0)
                return BadRequest("Debe cargar un archivo XML válido.");

            try
            {
                string xmlContent;
                using (var reader = new StreamReader(archivoXml.OpenReadStream()))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }

                var doc = XDocument.Parse(xmlContent);
                var idDoc = doc.Descendants("IdDoc").FirstOrDefault();
                var emisor = doc.Descendants("Emisor").FirstOrDefault();
                var comprador = doc.Descendants("Comprador").FirstOrDefault();
                var totales = doc.Descendants("Totales").FirstOrDefault();
                var InformacionReferencia = doc.Descendants("InformacionReferencia").FirstOrDefault();
                var DescuentoORecargo = doc.Descendants("DescuentoORecargo").FirstOrDefault();
                var items = doc.Descendants("Item").ToList();

                var firma = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "SignatureValue")?.Value ?? "";

                var fechaFirma = doc.Descendants("FechaHoraFirma").FirstOrDefault()?.Value ?? "";
                var eNCF = idDoc?.Element("eNCF")?.Value ?? "";

                var NCFModificado = InformacionReferencia?.Element("NCFModificado")?.Value ?? "";
                var CodigoMoficacion = InformacionReferencia?.Element("CodigoModificacion")?.Value ?? "0";
                var RazonModificacion = InformacionReferencia?.Element("RazonModificacion")?.Value ?? "";
                var fechaEmision = emisor?.Element("FechaEmision")?.Value ?? "";
                var rncEmisor = emisor?.Element("RNCEmisor")?.Value ?? "SINRNC";


                //descuentos o recargo 
                var MontoDescuentooRecargo = DescuentoORecargo?.Element("MontoDescuentooRecargo")?.Value ?? "0";
                // Extraer posibles valores del comprador
                string rncComprador = comprador?.Element("RNCComprador")?.Value;
                string idExtranjero = comprador?.Element("IdentificadorExtranjero")?.Value;

                // Elegir el valor correcto
                string valorrncComprador = !string.IsNullOrWhiteSpace(rncComprador) ? rncComprador : !string.IsNullOrWhiteSpace(idExtranjero) ? idExtranjero : null;

                // Generar QR
                //string urlQR = "https://fc.dgii.gov.do/testecf/consultatimbrefc?" +
                //$"RncEmisor={Utils.Utils.ReemplazarCaracteresQR(rncEmisor)}" +
                //$"&ENCF={Utils.Utils.ReemplazarCaracteresQR(eNCF)}" +
                //$"&MontoTotal={totales?.Element("MontoTotal")?.Value?.Replace(",", ".") ?? ""}" +
                ////$"&MontoTotal={Utils.Utils.ReemplazarCaracteresQR(totales?.Element("MontoTotal")?.Value ?? "")}" +
                //$"&CodigoSeguridad={Utils.Utils.ReemplazarCaracteresQR(firma.Substring(0, 6))}";

                DateTime fechaEmisionDt = DateTime.ParseExact(fechaEmision, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                string fechaEmisionQR = fechaEmisionDt.ToString("dd-MM-yyyy");

                DateTime fechaFirmaDt = DateTime.ParseExact(fechaFirma, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                string fechaFirmaQR = fechaFirmaDt.ToString("dd-MM-yyyy HH:mm:ss");

                string montoTotal = totales?.Element("MontoTotal")?.Value?.Replace(",", ".") ?? "";

                string urlQR =
                    "https://fc.dgii.gov.do/certecf/consultatimbrefc?" +
                    $"RncEmisor={Utils.Utils.ReemplazarCaracteresQR(rncEmisor)}" +
                    $"&RncComprador={Utils.Utils.ReemplazarCaracteresQR(valorrncComprador ?? "")}" +
                    $"&ENCF={Utils.Utils.ReemplazarCaracteresQR(eNCF)}" +
                    $"&FechaEmision={fechaEmisionQR}" +
                    $"&MontoTotal={montoTotal}" +
                    $"&FechaFirma={Utils.Utils.ReemplazarCaracteresQR(fechaFirmaQR)}" +
                    $"&CodigoSeguridad={Utils.Utils.ReemplazarCaracteresQR(firma.Substring(0, 6))}";


                // string urlQR = $"https://fc.dgii.gov.do/testecf/consultatimbrefc?" +
                //$"RncEmisor={rncEmisor}" +
                //$"&ENCF={eNCF}" +
                //$"&MontoTotal={totales?.Element("MontoTotal")?.Value}" +
                //$"&CodigoSeguridad={firma.Substring(0, 6)}";

                PdfDocument pdf = new PdfDocument();
                PdfPage page = pdf.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Arial", 12, XFontStyle.Regular);
                XFont font10 = new XFont("Arial", 10, XFontStyle.Regular);
                XFont boldFont = new XFont("Arial", 12, XFontStyle.Bold);
                XFont boldFont11 = new XFont("Arial", 11, XFontStyle.Bold);
                XPen bluePen = new XPen(XColors.Blue, 2);

                int yPoint = 0;
                int leftMargin = 10;
                int rightMargin = 10;
                double usableWidth = page.Width - leftMargin - rightMargin;
                double anchoMaxDireccion = usableWidth * 0.6;

                // Encabezado
                yPoint += 20;

                gfx.DrawString($"{emisor.Element("RazonSocialEmisor")?.Value}", boldFont, XBrushes.Black,
                 new XRect(leftMargin, yPoint, usableWidth * 0.6, 20), XStringFormats.TopLeft);
                gfx.DrawString($"RNC: {rncEmisor}", font, XBrushes.Black,
                    new XRect(leftMargin, yPoint + 20, usableWidth * 0.6, 20), XStringFormats.TopLeft);

                // 2. Dirección con formato controlado
                string direccion = $"Dirección: {emisor.Element("DireccionEmisor")?.Value}";
                var direccionLines = SplitTextByLength(direccion, 60);
                for (int i = 0; i < direccionLines.Length; i++)
                {
                    gfx.DrawString(direccionLines[i], font10, XBrushes.Black,
                        new XRect(leftMargin, yPoint + 40 + (i * 15), usableWidth * 0.6, 15),
                        XStringFormats.TopLeft);
                }

                // 3. Fecha de emisión (se ajusta según líneas de dirección)
                int fechaYPosition = yPoint + 40 + (direccionLines.Length * 15);
                gfx.DrawString($"Fecha emision: {emisor.Element("FechaEmision")?.Value}", font, XBrushes.Black,
                    new XRect(leftMargin, fechaYPosition, usableWidth * 0.6, 20), XStringFormats.TopLeft);


                //saco el nombre del comprobante 
                string NombreNCF = GetNombreComprobante(Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0"));

                // Datos fiscales (derecha)
                gfx.DrawString(NombreNCF, boldFont, XBrushes.Blue, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopRight);
                yPoint += 10;

                //funcion para pasarle el titulo en negrita y el valor normal
                TituloConDescri(gfx, "eNCF: ", eNCF, boldFont, font, leftMargin, yPoint + 20, usableWidth);

                //muestro el ncf modificado para notas de debito / credito
                if (!string.IsNullOrEmpty(NCFModificado.ToString()))
                {
                    TituloConDescri(gfx, "NCF Modificado: ", NCFModificado, boldFont, font, leftMargin, yPoint + 40, usableWidth);
                }

                //solo muestro la fecha de vencimiento(si esta incluida) o si no es una nota de debito / credito
                //string FechaVencimientoNCF = idDoc.Element("FechaVencimientoSecuencia")?.Value ?? "";
                //if (!string.IsNullOrEmpty(FechaVencimientoNCF) && Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") != 33 && Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") != 34)
                //{
                //    TituloConDescri(gfx, "Fecha Vencimiento: ", FechaVencimientoNCF, boldFont, font, leftMargin, yPoint + 40, usableWidth);
                //}

                //la razon de la nota de debito / credito 
                if (!string.IsNullOrEmpty(RazonModificacion))
                {
                    gfx.DrawString(RazonModificacion, boldFont, XBrushes.Black, new XRect(leftMargin, yPoint + 50, usableWidth, 20), XStringFormats.TopRight);
                }
                else //si esta vacia la razon de modificacion que verifique si tiene el codigo y la coja de la funcion 
                {
                    //si es una nota y no otro comprobante
                    if (Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") == 33 || Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") == 34)
                    {
                        gfx.DrawString(GetNombreCodigoMoficacionNCF(Int32.Parse(CodigoMoficacion.ToString())), boldFont, XBrushes.Black, new XRect(leftMargin, yPoint + 50, usableWidth, 20), XStringFormats.TopRight);
                    }
                }


                yPoint += 100; // espacio para ambos bloques

                //yPoint += 30;

                // Línea azul arriba
                gfx.DrawLine(bluePen, leftMargin, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 5;

                // Comprador
                //lo muestro solo si no es gasto menor
                if (Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") != 43)
                {
                    gfx.DrawString($"Razon social cliente: {comprador.Element("RazonSocialComprador")?.Value}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                    yPoint += 20;
                    gfx.DrawString($"RNC Cliente: {valorrncComprador}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                    yPoint += 20;

                    // Línea azul abajo
                    gfx.DrawLine(bluePen, leftMargin, yPoint, page.Width - rightMargin, yPoint);
                    yPoint += 25;
                }

                // Tabla detalle encabezado
                int colX_Cantidad = leftMargin;
                int colX_Exento = leftMargin + 55;
                int colX_Descripcion = colX_Cantidad + 70;
                int colX_Unidad = colX_Descripcion + 200;
                int colX_Precio = colX_Unidad + 70; //70
                int colX_ITBIS = colX_Precio + 70; //50 cambiar para el E46 de la prueba
                int colX_Total = colX_ITBIS + 70; //90

                gfx.DrawString("Cantidad", boldFont, XBrushes.Black, new XRect(colX_Cantidad, yPoint, 50, 20), XStringFormats.TopLeft);
                //gfx.DrawString("", boldFont, XBrushes.Black, new XRect(colX_Cantidad, yPoint, 60, 20), XStringFormats.TopLeft);
                gfx.DrawString("Descripción", boldFont, XBrushes.Black, new XRect(colX_Descripcion, yPoint, 200, 20), XStringFormats.TopLeft);
                gfx.DrawString("Unidad", boldFont, XBrushes.Black, new XRect(colX_Unidad, yPoint, 80, 10), XStringFormats.TopLeft);
                gfx.DrawString("de medida", boldFont, XBrushes.Black, new XRect(colX_Unidad, yPoint + 10, 80, 10), XStringFormats.TopLeft);

                gfx.DrawString("Precio", boldFont, XBrushes.Black, new XRect(colX_Precio, yPoint, 80, 20), XStringFormats.TopRight);
                gfx.DrawString("ITBIS", boldFont, XBrushes.Black, new XRect(colX_ITBIS, yPoint, 80, 20), XStringFormats.TopRight);
                gfx.DrawString("Valor", boldFont, XBrushes.Black, new XRect(colX_Total, yPoint, 80, 20), XStringFormats.TopRight);
                yPoint += 25;
                gfx.DrawLine(XPens.Black, colX_Cantidad, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 5;
                decimal TotalExento = 0;


                // Altura de cada línea del detalle
                int altoLinea = 20;
                // Margen inferior mínimo para que se vaya a la siguiente pagina 
                double margenInferior = 30;

                foreach (var item in items)
                {
                    // Verifica si queda espacio para otra línea
                    if (yPoint + altoLinea > page.Height - margenInferior)
                    {
                        // Crear nueva página
                        page = pdf.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        gfx = XGraphics.FromPdfPage(page);

                        yPoint = 40; // Reiniciar desde la parte superior (sin encabezado si no lo deseas)
                    }

                    var cantidad = item.Element("CantidadItem")?.Value ?? "";
                    var nombre = item.Element("NombreItem")?.Value ?? "";
                    var unidad = item.Element("UnidadMedida")?.Value ?? "";
                    var precioUnitario = item.Element("PrecioUnitarioItem")?.Value ?? "0";
                    var montoItem = item.Element("MontoItem")?.Value ?? "0";
                    var IndicadorFacturacion = item.Element("IndicadorFacturacion")?.Value ?? "0"; //elIndicador de facturacion es el tipo de itbis(0= no facturable, 4 = Exento)
                    decimal PorcientoItbis = IndicadorFacturacion switch
                    {
                        "1" => 18m,
                        "2" => 16m,
                        "3" => 0m,
                        _ => 0m // para valores como "4", "0", null, etc.
                    };

                    //si es un producto exento
                    string muestraE = "";
                    if (Int32.Parse(IndicadorFacturacion) == 4)
                    {
                        TotalExento += decimal.Parse(cantidad) * decimal.Parse(precioUnitario);
                        muestraE = "E";
                    }

                    decimal Precio = decimal.Parse(precioUnitario);
                    decimal itbis = Precio * (PorcientoItbis / 100);

                    string UnidadDescripcion = _querySqlService.ObtenerDescriUnidad(unidad.ToString());

                    gfx.DrawString(cantidad, font, XBrushes.Black, new XRect(colX_Cantidad, yPoint, 50, 20), XStringFormats.TopLeft);
                    if (muestraE != "")
                    {
                        gfx.DrawString(muestraE, boldFont11, XBrushes.Black, new XRect(colX_Exento, yPoint, 170, 20), XStringFormats.TopLeft);
                    }

                    gfx.DrawString(nombre, font, XBrushes.Black, new XRect(colX_Descripcion, yPoint, 200, 20), XStringFormats.TopLeft);
                    gfx.DrawString(UnidadDescripcion, font, XBrushes.Black, new XRect(colX_Unidad, yPoint, 60, 20), XStringFormats.TopLeft);
                    gfx.DrawString(Convert.ToDecimal(precioUnitario).ToString("N2"), font, XBrushes.Black, new XRect(colX_Precio, yPoint, 80, 20), XStringFormats.TopRight);
                    gfx.DrawString(itbis.ToString("N2"), font, XBrushes.Black, new XRect(colX_ITBIS, yPoint, 80, 20), XStringFormats.TopRight);
                    gfx.DrawString(Convert.ToDecimal(montoItem).ToString("N2"), font, XBrushes.Black, new XRect(colX_Total, yPoint, 80, 20), XStringFormats.TopRight);
                    yPoint += altoLinea;
                }

                //linea debajo del detalle
                gfx.DrawLine(XPens.Black, colX_Cantidad, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 5;
                // Base para totales
                int baseY = yPoint + 20;

                int sumalto = 0;
                var montoGravadoStr = totales.Element("MontoGravadoTotal")?.Value;
                if ((decimal.TryParse(montoGravadoStr, out decimal montoGravado) && montoGravado > 0) || TotalExento == 0)
                {
                    TituloConDescri(gfx, "Subtotal gravado: ", montoGravado.ToString("N2"), boldFont, font, rightMargin, yPoint + 20, usableWidth - 18);

                    sumalto = TotalExento > 0 ? 20 : 0; //para si esta el totalexento que deje un espacio 
                }


                if (TotalExento > 0)
                {
                    //yPoint += 20; // sisguiente linea
                    TituloConDescri(gfx, "Subtotal exento: ", TotalExento.ToString("N2"), boldFont, font, rightMargin, yPoint + 20 + sumalto, usableWidth - 18);
                    sumalto = Convert.ToDecimal(MontoDescuentooRecargo) > 0 ? 20 : 0;
                }

                if (Convert.ToDecimal(MontoDescuentooRecargo) > 0)
                {

                    TituloConDescri(gfx, "Total Descuento: ", Convert.ToDecimal(MontoDescuentooRecargo).ToString("N2"), boldFont, font, rightMargin, yPoint + 20 + sumalto, usableWidth - 18);
                }

                TituloConDescri(gfx, "Total ITBIS: ", Convert.ToDecimal(totales.Element("TotalITBIS")?.Value ?? "0").ToString("N2"), boldFont, font, rightMargin, yPoint + 40 + sumalto, usableWidth - 18);
                TituloConDescri(gfx, "Monto Total: ", Convert.ToDecimal(totales.Element("MontoTotal")?.Value ?? "0").ToString("N2"), boldFont, font, rightMargin, yPoint + 60 + sumalto, usableWidth - 18);

                yPoint += 20;

                // QR
                var qrGenerator = new QRCodeGenerator();
                var qrData = qrGenerator.CreateQrCode(urlQR, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.BitmapByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(20);

                using (var qrStream = new MemoryStream(qrBytes))
                {
                    XImage qrImage = XImage.FromStream(() => new MemoryStream(qrBytes));
                    gfx.DrawImage(qrImage, 20, yPoint + 40, 130, 130);
                }

                yPoint += 170;
                gfx.DrawString($"Codigo de seguridad: {firma.Substring(0, 6)}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                yPoint += 20;
                gfx.DrawString($"Fecha firma: {fechaFirma}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);

                

                //Por este...
                try
                {
                    // Limpia el RNC de caracteres no válidos
                    string rncLimpio = Utils.Utils.LimpiarRNC(rncEmisor);
                    string fileName = $"{rncLimpio}{eNCF}.pdf";
                    string dynamicFolderPath = _fileStorageManager.GetDynamicFolderPath(
                        rncLimpio,
                        FileStorageManager.StorageType.RIPdfs
                    );
                    string fullPath = Path.Combine(dynamicFolderPath, fileName);
                    pdf.Save(fullPath);

                    string pdfUrl = $"{Request.Scheme}://{Request.Host}/Storage/RIPdfs/{rncLimpio}/{fileName}";
                    return new JsonResult(new
                    {
                        PdfUrl = pdfUrl,
                        QrUrl = urlQR
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error al guardar el PDF: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generando PDF: {ex.Message}");
            }
        }

        [HttpPost("generar-pdfcompras")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GenerarPdfCompras([FromForm] UploadXmlRequest request)
        {
            var archivoXml = request.ArchivoXml;
            if (archivoXml == null || archivoXml.Length == 0)
                return BadRequest("Debe cargar un archivo XML válido.");

            try
            {
                string xmlContent;
                using (var reader = new StreamReader(archivoXml.OpenReadStream()))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }

                var doc = XDocument.Parse(xmlContent);
                var idDoc = doc.Descendants("IdDoc").FirstOrDefault();
                var emisor = doc.Descendants("Emisor").FirstOrDefault();
                var comprador = doc.Descendants("Comprador").FirstOrDefault();
                var totales = doc.Descendants("Totales").FirstOrDefault();
                var InformacionReferencia = doc.Descendants("InformacionReferencia").FirstOrDefault();

                var items = doc.Descendants("Item").ToList();

                var firma = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "SignatureValue")?.Value ?? "";

                var fechaFirma = doc.Descendants("FechaHoraFirma").FirstOrDefault()?.Value ?? "";
                var eNCF = idDoc?.Element("eNCF")?.Value ?? "";

                var NCFModificado = InformacionReferencia?.Element("NCFModificado")?.Value ?? "";
                var CodigoMoficacion = InformacionReferencia?.Element("CodigoModificacion")?.Value ?? "0";
                var RazonModificacion = InformacionReferencia?.Element("RazonModificacion")?.Value ?? "";
                var fechaEmision = emisor?.Element("FechaEmision")?.Value ?? "";
                var rncEmisor = emisor?.Element("RNCEmisor")?.Value ?? "SINRNC";

                // Generar QR
                string urlQR = $"https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?" +
                               $"RncEmisor={rncEmisor}" +
                               $"&RncComprador={comprador?.Element("RNCComprador")?.Value}" +
                               $"&ENCF={eNCF}" +
                               $"&FechaEmision={fechaEmision}" +
                               $"&MontoTotal={totales?.Element("MontoTotal")?.Value}" +
                               $"&FechaFirma={fechaFirma.Replace(" ", "+")}" +
                               $"&CodigoSeguridad={firma.Substring(0, 6)}";


                PdfDocument pdf = new PdfDocument();
                PdfPage page = pdf.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Arial", 12, XFontStyle.Regular);
                XFont font10 = new XFont("Arial", 10, XFontStyle.Regular);
                XFont boldFont = new XFont("Arial", 12, XFontStyle.Bold);
                XFont boldFont11 = new XFont("Arial", 11, XFontStyle.Bold);
                XPen bluePen = new XPen(XColors.Blue, 2);

                int yPoint = 0;
                int leftMargin = 10;
                int rightMargin = 10;
                double usableWidth = page.Width - leftMargin - rightMargin;
                double anchoMaxDireccion = usableWidth * 0.6;

                // Encabezado
                yPoint += 20;

                // Datos del emisor (izquierda)
                gfx.DrawString($"{emisor.Element("RazonSocialEmisor")?.Value}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                //gfx.DrawString($"RNC: {rncEmisor}", font, XBrushes.Black, new XRect(leftMargin, yPoint + 40, usableWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString($"RNC: {rncEmisor}", font, XBrushes.Black, new XRect(leftMargin, yPoint + 20, usableWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString($"Dirección: {emisor.Element("DireccionEmisor")?.Value}", font10, XBrushes.Black, new XRect(leftMargin, yPoint + 40, usableWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString($"Fecha emision: {emisor.Element("FechaEmision")?.Value}", font, XBrushes.Black, new XRect(leftMargin, yPoint + 60, usableWidth, 20), XStringFormats.TopLeft);


                //var direccion = emisor.Element("DireccionEmisor")?.Value ?? "";
                //var textFormatter = new XTextFormatter(gfx);

                //XRect rectDireccion = new XRect(leftMargin, yPoint + 40, anchoMaxDireccion, page.Height - yPoint - 40);
                //textFormatter.DrawString($"Dirección: {direccion}", font, XBrushes.Black, rectDireccion);


                //saco el nombre del comprobante 
                string NombreNCF = GetNombreComprobante(Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0"));

                // Datos fiscales (derecha)
                gfx.DrawString(NombreNCF, boldFont, XBrushes.Blue, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopRight);
                yPoint += 10;

                //funcion para pasarle el titulo en negrita y el valor normal
                TituloConDescri(gfx, "eNCF: ", eNCF, boldFont, font, leftMargin, yPoint + 20, usableWidth);

                //muestro el ncf modificado para notas de debito / credito
                if (!string.IsNullOrEmpty(NCFModificado.ToString()))
                {
                    TituloConDescri(gfx, "NCF Modificado: ", NCFModificado, boldFont, font, leftMargin, yPoint + 40, usableWidth);
                }


                //solo muestro la fecha de vencimiento(si esta incluida) o si no es una nota de debito / credito
                string FechaVencimientoNCF = idDoc.Element("FechaVencimientoSecuencia")?.Value ?? "";
                if (!string.IsNullOrEmpty(FechaVencimientoNCF) && Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") != 33 && Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") != 34)
                {
                    TituloConDescri(gfx, "Fecha Vencimiento: ", FechaVencimientoNCF, boldFont, font, leftMargin, yPoint + 40, usableWidth);
                }

                //la razon de la nota de debito / credito 
                if (!string.IsNullOrEmpty(RazonModificacion))
                {
                    gfx.DrawString(RazonModificacion, boldFont, XBrushes.Black, new XRect(leftMargin, yPoint + 50, usableWidth, 20), XStringFormats.TopRight);
                }
                else //si esta vacia la razon de modificacion que verifique si tiene el codigo y la coja de la funcion 
                {
                    //si es una nota y no otro comprobante
                    if (Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") == 33 || Convert.ToInt32(idDoc.Element("TipoeCF")?.Value ?? "0") == 34)
                    {
                        gfx.DrawString(GetNombreCodigoMoficacionNCF(Int32.Parse(CodigoMoficacion.ToString())), boldFont, XBrushes.Black, new XRect(leftMargin, yPoint + 50, usableWidth, 20), XStringFormats.TopRight);
                    }
                }


                yPoint += 70; // espacio para ambos bloques

                yPoint += 30;

                // Línea azul arriba
                gfx.DrawLine(bluePen, leftMargin, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 5;

                // Comprador
                gfx.DrawString($"Razon social cliente: {comprador.Element("RazonSocialComprador")?.Value}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                yPoint += 20;
                gfx.DrawString($"RNC Cliente: {comprador.Element("RNCComprador")?.Value}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                yPoint += 20;

                // Línea azul abajo
                gfx.DrawLine(bluePen, leftMargin, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 25;

                // Tabla detalle encabezado
                int colX_Cantidad = leftMargin;
                int colX_Exento = leftMargin + 55;
                int colX_Descripcion = colX_Cantidad + 70;
                int colX_Unidad = colX_Descripcion + 200;
                int colX_Precio = colX_Unidad + 70;
                int colX_ITBIS = colX_Precio + 70;
                int colX_Total = colX_ITBIS + 70;

                gfx.DrawString("Cantidad", boldFont, XBrushes.Black, new XRect(colX_Cantidad, yPoint, 50, 20), XStringFormats.TopLeft);
                //gfx.DrawString("", boldFont, XBrushes.Black, new XRect(colX_Cantidad, yPoint, 60, 20), XStringFormats.TopLeft);
                gfx.DrawString("Descripción", boldFont, XBrushes.Black, new XRect(colX_Descripcion, yPoint, 200, 20), XStringFormats.TopLeft);
                gfx.DrawString("Unidad", boldFont, XBrushes.Black, new XRect(colX_Unidad, yPoint, 80, 10), XStringFormats.TopLeft);
                gfx.DrawString("de medida", boldFont, XBrushes.Black, new XRect(colX_Unidad, yPoint + 10, 80, 10), XStringFormats.TopLeft);

                gfx.DrawString("Precio", boldFont, XBrushes.Black, new XRect(colX_Precio, yPoint, 80, 20), XStringFormats.TopRight);
                gfx.DrawString("ITBIS", boldFont, XBrushes.Black, new XRect(colX_ITBIS, yPoint, 80, 20), XStringFormats.TopRight);
                gfx.DrawString("Valor", boldFont, XBrushes.Black, new XRect(colX_Total, yPoint, 80, 20), XStringFormats.TopRight);
                yPoint += 25;
                gfx.DrawLine(XPens.Black, colX_Cantidad, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 5;
                decimal TotalExento = 0;

                foreach (var item in items)
                {
                    var cantidad = item.Element("CantidadItem")?.Value ?? "";
                    var nombre = item.Element("NombreItem")?.Value ?? "";
                    var unidad = item.Element("UnidadMedida")?.Value ?? "";
                    var precioUnitario = item.Element("PrecioUnitarioItem")?.Value ?? "0";
                    var montoItem = item.Element("MontoItem")?.Value ?? "0";
                    var IndicadorFacturacion = item.Element("IndicadorFacturacion")?.Value ?? "0"; //elIndicador de facturacion es el tipo de itbis(0= no facturable, 4 = Exento)
                    decimal PorcientoItbis = IndicadorFacturacion switch
                    {
                        "1" => 18m,
                        "2" => 16m,
                        "3" => 0m,
                        _ => 0m // para valores como "4", "0", null, etc.
                    };

                    //si es un producto exento
                    string muestraE = "";
                    if (Int32.Parse(IndicadorFacturacion) == 4)
                    {
                        TotalExento += decimal.Parse(cantidad) * decimal.Parse(precioUnitario);
                        muestraE = "E";
                    }

                    decimal Precio = decimal.Parse(precioUnitario);
                    decimal itbis = Precio * (PorcientoItbis / 100);

                    string UnidadDescripcion = _querySqlService.ObtenerDescriUnidad(unidad.ToString());

                    gfx.DrawString(cantidad, font, XBrushes.Black, new XRect(colX_Cantidad, yPoint, 50, 20), XStringFormats.TopLeft);
                    if (muestraE != "")
                    {
                        gfx.DrawString(muestraE, boldFont11, XBrushes.Black, new XRect(colX_Exento, yPoint, 170, 20), XStringFormats.TopLeft);
                    }

                    gfx.DrawString(nombre, font, XBrushes.Black, new XRect(colX_Descripcion, yPoint, 200, 20), XStringFormats.TopLeft);
                    gfx.DrawString(UnidadDescripcion, font, XBrushes.Black, new XRect(colX_Unidad, yPoint, 60, 20), XStringFormats.TopLeft);
                    gfx.DrawString(precioUnitario, font, XBrushes.Black, new XRect(colX_Precio, yPoint, 80, 20), XStringFormats.TopRight);
                    gfx.DrawString(itbis.ToString("N2"), font, XBrushes.Black, new XRect(colX_ITBIS, yPoint, 80, 20), XStringFormats.TopRight);
                    gfx.DrawString(Convert.ToDecimal(montoItem).ToString("N2"), font, XBrushes.Black, new XRect(colX_Total, yPoint, 80, 20), XStringFormats.TopRight);
                    yPoint += 20;
                }

                //linea debajo del detalle
                gfx.DrawLine(XPens.Black, colX_Cantidad, yPoint, page.Width - rightMargin, yPoint);
                yPoint += 5;

                int sumalto = 0;
                var montoGravadoStr = totales.Element("MontoGravadoTotal")?.Value;
                if ((decimal.TryParse(montoGravadoStr, out decimal montoGravado) && montoGravado > 0) || TotalExento == 0)
                {
                    TituloConDescri(gfx, "Subtotal gravado: ", montoGravado.ToString("N2"), boldFont, font, rightMargin, yPoint + 20, usableWidth - 18);

                    sumalto = TotalExento > 0 ? 20 : 0; //para si esta el totalexento que deje un espacio 
                }


                //TituloConDescri(gfx, "Subtotal gravado: ",Convert.ToDecimal(totales.Element("MontoGravadoTotal")?.Value ?? "0").ToString("N2"), boldFont, font, rightMargin, yPoint + 20, usableWidth -18);
                //sumalto = 20;

                if (TotalExento > 0)
                {
                    //yPoint += 20; // sisguiente linea
                    TituloConDescri(gfx, "Monto exento: ", TotalExento.ToString("N2"), boldFont, font, rightMargin, yPoint + 20 + sumalto, usableWidth - 18);
                    //sumalto += 20;
                }
                TituloConDescri(gfx, "Total ITBIS: ", Convert.ToDecimal(totales.Element("TotalITBIS")?.Value ?? "0").ToString("N2"), boldFont, font, rightMargin, yPoint + 40 + sumalto, usableWidth - 18);
                TituloConDescri(gfx, "Monto Total: ", Convert.ToDecimal(totales.Element("MontoTotal")?.Value ?? "0").ToString("N2"), boldFont, font, rightMargin, yPoint + 60 + sumalto, usableWidth - 18);

                yPoint += 20;

                // QR
                var qrGenerator = new QRCodeGenerator();
                var qrData = qrGenerator.CreateQrCode(urlQR, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.BitmapByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(20);

                //descomentar para el tamano que establece la DGII(como minimo)
                //using (var qrStream = new MemoryStream(qrBytes))
                //{
                //    XImage qrImage = XImage.FromStream(() => new MemoryStream(qrBytes));

                //    // Conversión de medidas
                //    double qrWidth = 22 * 2.83465; // ≈ 62.36 pt
                //    double qrMargin = 3 * 2.83465; // ≈ 8.5 pt

                //    // Ajustar posición (ejemplo)
                //    double posX = leftMargin + qrMargin;
                //    double posY = yPoint + qrMargin;

                //    // Dibujar QR con tamaño exacto
                //    gfx.DrawImage(qrImage, posX, posY, qrWidth, qrWidth);
                //}

                using (var qrStream = new MemoryStream(qrBytes))
                {
                    XImage qrImage = XImage.FromStream(() => new MemoryStream(qrBytes));
                    gfx.DrawImage(qrImage, 20, yPoint + 40, 130, 130);
                }

                yPoint += 170;
                gfx.DrawString($"Codigo de seguridad: {firma.Substring(0, 6)}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);
                yPoint += 20;
                gfx.DrawString($"Fecha firma: {fechaFirma}", font, XBrushes.Black, new XRect(leftMargin, yPoint, usableWidth, 20), XStringFormats.TopLeft);

                // Para aplicar las rutas dinamica se elimino esto.

                //string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "FacturasPDF");
                //if (!Directory.Exists(folderPath))
                //    Directory.CreateDirectory(folderPath);

                //string fileName = $"{rncEmisor.ToString()}{eNCF.ToString()}.pdf";
                //string fullPath = Path.Combine(folderPath, fileName);
                //pdf.Save(fullPath);

                //Por este cuerpo en el cual se aplica la rutas dinamicas
                string fileName = $"{rncEmisor}{eNCF}.pdf";
                string rncEmisorValido = Utils.Utils.LimpiarRNC(rncEmisor);
                // Guarda el PDF en la carpeta dinámica Storage/RIPdfs/{RNC}/
                string fullPath = Path.Combine(
                    _fileStorageManager.GetDynamicFolderPath(rncEmisorValido, FileStorageManager.StorageType.RIPdfs),
                    fileName
                );
                pdf.Save(fullPath);


                byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generando PDF: {ex.Message}");
            }
        }


        [HttpPost("procesar-todos-los-pdf")]
        public async Task<IActionResult> ProcesarTodosLosPDF([FromQuery] string rnc)
        {
            try
            {
                string baseRuta = Path.Combine("Storage", "Certificacion", "Simulacion", "Facturas", rnc);

                if (!Directory.Exists(baseRuta))
                    return NotFound("La carpeta del RNC no existe.");

                var subcarpetas = Directory.GetDirectories(baseRuta);

                if (subcarpetas.Length == 0)
                    return NotFound("No hay carpetas de tipo de factura dentro del RNC.");

                var resultados = new List<ResultadoGeneracionPDFDTO>();
                var client = _httpClientFactory.CreateClient("ApiClient");

                foreach (var carpeta in subcarpetas)
                {
                    var archivosXml = Directory.GetFiles(carpeta, "*.xml");

                    foreach (var archivo in archivosXml)
                    {
                        var nombreArchivo = Path.GetFileName(archivo);

                        try
                        {
                            // Leer el XML para determinar el tipo y monto
                            var xmlContent = await System.IO.File.ReadAllTextAsync(archivo);
                            var doc = XDocument.Parse(xmlContent);
                            var idDoc = doc.Descendants("IdDoc").FirstOrDefault();
                            var totales = doc.Descendants("Totales").FirstOrDefault();

                            int tipoECF = Convert.ToInt32(idDoc?.Element("TipoeCF")?.Value ?? "0");
                            decimal montoTotal = Convert.ToDecimal(totales?.Element("MontoTotal")?.Value ?? "0");

                            string endpoint;

                            // Determinar qué endpoint usar
                            if (tipoECF == 32 && montoTotal < 250000)
                            {
                                endpoint = "/api/RepresentacionImpresa/generar-pdf-E32";
                            }
                            else
                            {
                                endpoint = "/api/RepresentacionImpresa/generar-pdf";
                            }

                            using var stream = System.IO.File.OpenRead(archivo);
                            using var content = new MultipartFormDataContent();
                            content.Add(new StreamContent(stream), "archivoXml", nombreArchivo);

                            var response = await client.PostAsync(endpoint, content);

                            if (response.IsSuccessStatusCode)
                            {
                                var responseContent = await response.Content.ReadAsStringAsync();
                                var resultado = JsonSerializer.Deserialize<ResultadoGeneracionPDFDTO>(responseContent, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                                resultado.NombreArchivo = nombreArchivo;
                                resultado.Exito = true;
                                resultado.Mensaje = "Procesado correctamente";

                                resultados.Add(resultado);
                            }
                            else
                            {
                                resultados.Add(new ResultadoGeneracionPDFDTO
                                {
                                    NombreArchivo = nombreArchivo,
                                    Exito = false,
                                    Mensaje = $"Fallo con status: {response.StatusCode}"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            resultados.Add(new ResultadoGeneracionPDFDTO
                            {
                                NombreArchivo = nombreArchivo,
                                Exito = false,
                                Mensaje = $"Error al procesar: {ex.Message}"
                            });
                        }
                    }
                }

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error procesando archivos: {ex.Message}");
            }
        }

        [HttpGet("listar-pdfs-con-qr")]
        public IActionResult ListarPDFsConQR([FromQuery] string rnc)
        {
            try
            {
                string baseRutaPdf = Path.Combine(_env.ContentRootPath, "Storage", "RI-PDFs", rnc);
                string baseRutaXml = Path.Combine(_env.ContentRootPath, "Storage", "Certificacion", "Simulacion", "Facturas", rnc);

                if (!Directory.Exists(baseRutaPdf))
                {
                    _logger.LogWarning("No existe carpeta de PDFs para el RNC {RNC}. Ruta esperada: {Ruta}", rnc, baseRutaPdf);
                    return NotFound("No existe carpeta de PDFs para ese RNC.");
                }

                var archivosPdf = Directory.GetFiles(baseRutaPdf, "*.pdf");
                _logger.LogInformation("Se encontraron {Count} archivos PDF para el RNC {RNC}.", archivosPdf.Length, rnc);

                var archivosXml = Directory.GetFiles(baseRutaXml, "*.xml", SearchOption.AllDirectories);
                _logger.LogInformation("Se encontraron {Count} archivos XML para el RNC {RNC}.", archivosXml.Length, rnc);

                var resultados = new List<ResultadoGeneracionPDFDTO>();

                foreach (var pdf in archivosPdf)
                {
                    var nombreArchivo = Path.GetFileName(pdf);
                    var nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);

                    var posibleXml = archivosXml
                        .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(nombreSinExtension, StringComparison.OrdinalIgnoreCase));

                    if (posibleXml == null)
                    {
                        _logger.LogWarning("No se encontró XML correspondiente para el PDF: {Archivo}", nombreArchivo);
                        continue;
                    }

                    var xmlDoc = XDocument.Load(posibleXml);
                    var root = xmlDoc.Root;

                    XNamespace ds = "http://www.w3.org/2000/09/xmldsig#";

                    var encabezado = root.Element("Encabezado");
                    var idDoc = encabezado?.Element("IdDoc");
                    var emisor = encabezado?.Element("Emisor");
                    var comprador = encabezado?.Element("Comprador");
                    var totales = encabezado?.Element("Totales");
                    var fechaHoraFirma = root.Element("FechaHoraFirma");
                    var signature = root.Element(ds + "Signature");

                    string rncEmisor = emisor?.Element("RNCEmisor")?.Value ?? "";
                    string rncComprador = comprador?.Element("RNCComprador")?.Value ?? "";
                    string eNCF = idDoc?.Element("eNCF")?.Value ?? "";
                    string fechaEmision = emisor?.Element("FechaEmision")?.Value ?? "";
                    string fechaFirma = fechaHoraFirma?.Value ?? "";

                    string codigoSeguridad = "";
                    if (signature != null)
                    {
                        var signatureValue = signature.Element(ds + "SignatureValue")?.Value ?? "";
                        codigoSeguridad = signatureValue.Length >= 6 ? signatureValue.Substring(0, 6) : signatureValue;
                    }
                    string montoTotal = totales?.Element("MontoTotal")?.Value ?? "";

                    int tipoECF = Convert.ToInt32(idDoc?.Element("TipoeCF")?.Value ?? "0");
                    decimal montoTotalDecimal = Convert.ToDecimal(montoTotal.Replace(",", ""));

                    //string qrUrl = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?" +
                    //    $"RncEmisor={Utils.Utils.ReemplazarCaracteresQR(rncEmisor)}" +
                    //    (!string.IsNullOrEmpty(rncComprador) ? $"&RncComprador={Utils.Utils.ReemplazarCaracteresQR(rncComprador)}" : "") +
                    //    $"&ENCF={Utils.Utils.ReemplazarCaracteresQR(eNCF)}" +
                    //    $"&FechaEmision={Utils.Utils.ReemplazarCaracteresQR(fechaEmision)}" +
                    //    $"&MontoTotal={Utils.Utils.ReemplazarCaracteresQR(montoTotal)}" +
                    //    $"&FechaFirma={Utils.Utils.ReemplazarCaracteresQR(fechaFirma)}" +
                    //    $"&CodigoSeguridad={Utils.Utils.ReemplazarCaracteresQR(codigoSeguridad)}";

                    string qrBaseUrl;
                    if (tipoECF == 32 && montoTotalDecimal < 250000)
                    {
                        // Usar el nuevo enlace para E32 con monto < 250,000
                        qrBaseUrl = "https://fc.dgii.gov.do/certecf/consultatimbrefc?" +
                            $"RncEmisor={Utils.Utils.ReemplazarCaracteresQR(rncEmisor)}" +
                            $"&ENCF={Utils.Utils.ReemplazarCaracteresQR(eNCF)}" +
                            $"&MontoTotal={Utils.Utils.ReemplazarCaracteresQR(montoTotal)}" +
                            $"&CodigoSeguridad={Utils.Utils.ReemplazarCaracteresQR(codigoSeguridad)}";
                    }
                    else
                    {
                        // Usar el enlace original para todos los demás casos
                        qrBaseUrl = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?" +
                            $"RncEmisor={Utils.Utils.ReemplazarCaracteresQR(rncEmisor)}" +
                            (!string.IsNullOrEmpty(rncComprador) ? $"&RncComprador={Utils.Utils.ReemplazarCaracteresQR(rncComprador)}" : "") +
                            $"&ENCF={Utils.Utils.ReemplazarCaracteresQR(eNCF)}" +
                            $"&FechaEmision={Utils.Utils.ReemplazarCaracteresQR(fechaEmision)}" +
                            $"&MontoTotal={Utils.Utils.ReemplazarCaracteresQR(montoTotal)}" +
                            $"&FechaFirma={Utils.Utils.ReemplazarCaracteresQR(fechaFirma)}" +
                            $"&CodigoSeguridad={Utils.Utils.ReemplazarCaracteresQR(codigoSeguridad)}";
                    }

                    resultados.Add(new ResultadoGeneracionPDFDTO
                    {
                        NombreArchivo = nombreArchivo,
                        PdfUrl = $"{Request.Scheme}://{Request.Host}/Storage/RI-PDFs/{rnc}/{nombreArchivo}",
                        QrUrl = qrBaseUrl,
                        Exito = true,
                        Mensaje = "Encontrado"
                    });
                }

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar los archivos PDF para el RNC {RNC}", rnc);
                return StatusCode(500, $"Error al listar los archivos PDF: {ex.Message}");
            }
        }

        [HttpGet("descargar-pdf")]
        public IActionResult DescargarPdf(string rnc, string nombreArchivo)
        {
            var ruta = Path.Combine("Storage", "RI-PDFs", rnc, nombreArchivo);

            if (!System.IO.File.Exists(ruta))
                return NotFound("El archivo no existe.");

            var bytes = System.IO.File.ReadAllBytes(ruta);
            return File(bytes, "application/pdf", nombreArchivo);
        }


        //funcion para poner el titulo en negrita y la descripcion en regular
        void TituloConDescri(XGraphics gfx, string label, string value, XFont labelFont, XFont valueFont, double xStart, double y, double totalWidth)
        {
            double labelWidth = gfx.MeasureString(label, labelFont).Width;
            double valueWidth = gfx.MeasureString(value, valueFont).Width;
            double x = xStart + totalWidth - (labelWidth + valueWidth);
            gfx.DrawString(label, labelFont, XBrushes.Black, new XPoint(x, y));
            gfx.DrawString(value, valueFont, XBrushes.Black, new XPoint(x + labelWidth, y));
        }
        //funcion para sacar el nombre del comprobante en base al tipo
        string GetNombreComprobante(int tipo)
        {
            string NombreNCF;

            switch (tipo)
            {
                case 31:
                    NombreNCF = "Factura de Crédito Fiscal Electrónica";
                    break;
                case 32:
                    NombreNCF = "Factura de Consumo Electrónica";
                    break;
                case 33:
                    NombreNCF = "Nota de Débito Electrónica";
                    break;
                case 34:
                    NombreNCF = "Nota de Crédito Electrónica";
                    break;
                case 41:
                    NombreNCF = "Comprobante Electrónico de Compras";
                    break;
                case 43:
                    NombreNCF = "Comprobante Electrónico para Gastos Menores";
                    break;
                case 44:
                    NombreNCF = "Comprobante Electrónico para Regímenes Especiales";
                    break;
                case 45:
                    NombreNCF = "Comprobante Electrónico Gubernamental";
                    break;
                case 46:
                    NombreNCF = "Comprobante Electrónico para Exportaciones";
                    break;
                case 47:
                    NombreNCF = "Comprobante Electrónico para Pagos al Exterior";
                    break;
                default:
                    NombreNCF = "Tipo desconocido";
                    break;
            }

            return NombreNCF;
        }

        //funcion para devolver los nombres de las modificaciones de los ncf
        string GetNombreCodigoMoficacionNCF(int codigo)
        {
            string NombreNCF;

            switch (codigo)
            {
                case 1:
                    NombreNCF = "Anula el NCF modificado";
                    break;
                case 2:
                    NombreNCF = "Corrige texto del Comprobante Fiscal Modificado";
                    break;
                case 3:
                    NombreNCF = "Corrige montos del NCF modificado";
                    break;
                case 4:
                    NombreNCF = "Reemplazo NCF emitido en contingencia";
                    break;
                case 5:
                    NombreNCF = "Referencia Factura Consumo Electrónica";
                    break;
                default:
                    NombreNCF = "codigo desconocido";
                    break;
            }

            return NombreNCF;
        }

        private void DrawWrappedText(XGraphics gfx, string text, XFont font, XBrush brush, XRect rect, bool rightAlign = false)
        {
            var format = new XStringFormat();
            format.Alignment = rightAlign ? XStringAlignment.Far : XStringAlignment.Near;
            format.LineAlignment = XLineAlignment.Near;

            var lines = new List<string>();
            var words = text.Split(' ');
            var currentLine = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                var testLine = currentLine.Length > 0 ? currentLine.ToString() + " " + word : word;
                var width = gfx.MeasureString(testLine, font).Width;

                if (width <= rect.Width)
                {
                    currentLine.Append(currentLine.Length > 0 ? " " + word : word);
                }
                else
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    currentLine.Append(word);
                }
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString());

            var lineHeight = font.GetHeight();
            var maxLines = (int)(rect.Height / lineHeight);

            for (int i = 0; i < Math.Min(lines.Count, maxLines); i++)
            {
                var y = rect.Top + (i * lineHeight);
                gfx.DrawString(lines[i], font, brush,
                              new XRect(rect.Left, y, rect.Width, lineHeight),
                              format);
            }
        }

        // Add this with your other utility methods
        private static string[] SplitTextByLength(string text, int maxLength)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < text.Length; i += maxLength)
            {
                int length = Math.Min(maxLength, text.Length - i);
                result.Add(text.Substring(i, length));
            }
            return result.ToArray();
        }
    }
}
