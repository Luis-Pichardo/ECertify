using Microsoft.AspNetCore.Mvc;

namespace eCertify.Models
{
    public class UploadXmlRequest
    {
        [FromForm(Name = "archivoXml")]
        public IFormFile ArchivoXml { get; set; }
    }
}
