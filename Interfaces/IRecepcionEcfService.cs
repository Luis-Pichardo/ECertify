namespace eCertify.Interfaces
{
    public interface IRecepcionEcfService
    {
        /// <summary>
        /// Procesa un archivo XML ECF y genera el ARECF correspondiente
        /// </summary>
        /// <param name="archivoXml">Archivo XML recibido</param>
        /// <returns>XML con el acuse de recibo</returns>
        Task<string> ProcesarEcfAsync(IFormFile archivoXml);

        /// <summary>
        /// Valida la estructura básica del archivo XML ECF
        /// </summary>
        /// <param name="archivoXml">Archivo a validar</param>
        /// <returns>Tupla con resultado y mensaje de error</returns>
        Task<(bool isValid, string errorMessage)> ValidarArchivoEcfAsync(IFormFile archivoXml);
    }
}
