using eCertify.Utils;

namespace eCertify.Interfaces
{
    public interface IFileStorageManager
    {
        //  static abstract string LimpiarRNC(string rnc);
        string GetBaseStoragePath();
        string GetDynamicFolderPath(string rnc, FileStorageManager.StorageType type);
        Task<string> SaveFileAsync(string rnc, FileStorageManager.StorageType type, IFormFile file, string fileName);
        Task SaveXmlAsync(string rnc, string xmlContent, string fileName, FileStorageManager.StorageType storageType = FileStorageManager.StorageType.Certificacion);
        Task<string> SaveFacturaXmlAsync(string rnc, string tipoFactura, string xmlContent, string fileName);
        Task<string> SaveResumenXmlAsync(string rnc, string xmlContent, string fileName);
        Task<string> SaveResumenExcelAsync(string rnc, string xmlContent, string fileName);
        Task<string> SaveSimulacionXmlAsync(string rnc, string tipoFactura, string xmlContent, string fileName);
        Task<string> SaveResumenSimulacionAsync(string rnc, string xmlContent, string fileName);
        Task<string> SaveARECFAsync(string rnc, string xmlContent, string relativePath, FileStorageManager.StorageType storageType);
        Task<string> SaveACECFAsync(string xmlContent, string rnc, string fileName);
        Task<string> GetFacturaXmlAsync(string rnc, string tipoFactura, string fileName);
        Task<string> GetResumenXmlAsync(string rnc, string fileName);

    }
}