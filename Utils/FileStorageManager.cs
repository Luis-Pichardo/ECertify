using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using eCertify.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eCertify.Utils
{
    public class FileStorageManager : IFileStorageManager
    {
        private readonly string _baseStoragePath;

        public FileStorageManager(IWebHostEnvironment env)
        {
            _baseStoragePath = Path.Combine(env.ContentRootPath, "Storage");
        }

        public enum StorageType
        {
            Aprobaciones,
            Certificados,
            Facturas,
            RIPdfs,
            PruebasExcel,
            Imagenes,
            Xmlfirmados,
            Certificacion
        }

        public string GetBaseStoragePath() => _baseStoragePath;

        public string GetDynamicFolderPath(string rnc, StorageType type)
        {
            string folderName = type switch
            {
                StorageType.RIPdfs => "RI-PDFs",
                StorageType.Certificacion => Path.Combine("Certificacion", "XMLGenerados"),
                StorageType.Aprobaciones => Path.Combine("Certificacion", "Aprobaciones"),
                _ => type.ToString()
            };

            string dynamicPath = Path.Combine(_baseStoragePath, folderName, rnc);

            if (!Directory.Exists(dynamicPath))
            {
                Directory.CreateDirectory(dynamicPath);
            }

            return dynamicPath;
        }

        public async Task<string> SaveFileAsync(string rnc, StorageType type, IFormFile file, string fileName)
        {
            string folderPath = GetDynamicFolderPath(rnc, type);
            string fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fullPath;
        }

        public async Task SaveXmlAsync(string rnc, string xmlContent, string fileName, StorageType storageType = StorageType.Certificacion)
        {
            string folderPath = GetDynamicFolderPath(rnc, storageType);
            string fullPath = Path.Combine(folderPath, fileName);
            await File.WriteAllTextAsync(fullPath, xmlContent);
        }

        public async Task<string> SaveFacturaXmlAsync(string rnc, string tipoFactura, string xmlContent, string fileName)
        {
            // Asegurar que el tipo tenga prefijo "E"
            string tipoFacturaNormalizado = tipoFactura.StartsWith("E") ? tipoFactura : "E" + tipoFactura;

            // Validar tipos de factura permitidos
            string[] tiposValidos = { "E31", "E32", "E33", "E34", "E41", "E43", "E44", "E45", "E46", "E47" };
            if (!tiposValidos.Contains(tipoFacturaNormalizado))
            {
                throw new ArgumentException($"Tipo de factura '{tipoFacturaNormalizado}' no es válido");
            }

            // Construir ruta: Storage/Facturas/RNC/TipoFactura
            string rutaCompleta = Path.Combine(_baseStoragePath, "Facturas", "Facturas", rnc, tipoFacturaNormalizado, fileName);

            // Crear directorios si no existen
            Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta));

            // Guardar archivo
            await System.IO.File.WriteAllTextAsync(rutaCompleta, xmlContent);

            return rutaCompleta;
        }


        /// <summary>
        /// Guarda resúmenes XML de facturas reales en el entorno de producción
        /// Ruta: Storage/Facturas/Resumenes/[RNC]/
        /// </summary>
        public async Task<string> SaveResumenXmlAsync(string rnc, string xmlContent, string fileName)
        {
            string carpetaResumenes = Path.Combine(_baseStoragePath, "Facturas", "Resumenes", rnc);

            if (!Directory.Exists(carpetaResumenes))
                Directory.CreateDirectory(carpetaResumenes);

            string rutaCompleta = Path.Combine(carpetaResumenes, fileName);

            await File.WriteAllTextAsync(rutaCompleta, xmlContent);

            return rutaCompleta;
        }

        public async Task<string> GetFacturaXmlAsync(string rnc, string tipoFactura, string fileName)
        {
            string tipoFacturaNormalizado = tipoFactura.StartsWith("E") ? tipoFactura : "E" + tipoFactura;
            string carpeta = Path.Combine(_baseStoragePath, "Facturas", "Facturas", rnc, tipoFacturaNormalizado);

            if (!Directory.Exists(carpeta))
                throw new DirectoryNotFoundException($"No se encontró la carpeta: {carpeta}");

            var archivos = Directory.GetFiles(carpeta, $"{rnc}*{fileName}*.xml");

            if (archivos.Length == 0)
                throw new FileNotFoundException($"No se encontró el XML de factura en la carpeta: {carpeta}");

            string ruta = archivos.First();

            return await File.ReadAllTextAsync(ruta);
        }


        public async Task<string> GetResumenXmlAsync(string rnc, string fileName)
        {
            string carpeta = Path.Combine(_baseStoragePath, "Facturas", "Resumenes", rnc);

            if (!Directory.Exists(carpeta))
                throw new DirectoryNotFoundException($"No se encontró la carpeta: {carpeta}");

            var archivos = Directory.GetFiles(carpeta, $"{rnc}*{fileName}*.xml");

            if (archivos.Length == 0)
                throw new FileNotFoundException($"No se encontró el XML de resumen en la carpeta: {carpeta}");

            string ruta = archivos.First();

            return await File.ReadAllTextAsync(ruta);
        }



        //// <summary>
        /// Guarda resúmenes Excel para pruebas de Datos e-CF/validación
        /// Ruta: Storage/Certificacion/Resumenes/[RNC]/
        /// </summary>
        public async Task<string> SaveResumenExcelAsync(string rnc, string xmlContent, string fileName)
        {
            string folderPath = Path.Combine(_baseStoragePath, "Certificacion", "Resumenes", rnc);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fullPath = Path.Combine(folderPath, fileName);
            await File.WriteAllTextAsync(fullPath, xmlContent);

            return fullPath;
        }

        public async Task<string> SaveSimulacionXmlAsync(string rnc, string tipoFactura, string xmlContent, string fileName)
        {
            // Validar tipos de factura permitidos
            string[] tiposValidos = { "E31", "E32", "E33", "E34", "E41", "E43", "E44", "E45", "E46", "E47" };
            if (!tiposValidos.Contains(tipoFactura))
            {
                throw new ArgumentException($"Tipo de factura '{tipoFactura}' no es válido");
            }

            // Construir ruta: Storage/Certificacion/Simulacion/Facturas/RNC/TipoFactura
            string rutaCompleta = Path.Combine(_baseStoragePath, "Certificacion", "Simulacion", "Facturas", rnc, tipoFactura, fileName);

            // Crear directorios si no existen
            Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta));

            // Guardar archivo
            await System.IO.File.WriteAllTextAsync(rutaCompleta, xmlContent);

            return rutaCompleta;
        }

        public async Task<string> SaveResumenSimulacionAsync(string rnc, string xmlContent, string fileName)
        {
            string carpetaResumenes = Path.Combine(_baseStoragePath, "Certificacion", "Simulacion", "Resumenes", rnc);

            if (!Directory.Exists(carpetaResumenes))
                Directory.CreateDirectory(carpetaResumenes);

            string rutaCompleta = Path.Combine(carpetaResumenes, fileName);

            await File.WriteAllTextAsync(rutaCompleta, xmlContent, new UTF8Encoding(false));

            return rutaCompleta;
        }

        public async Task<string> SaveARECFAsync(string rnc, string xmlContent, string relativePath, FileStorageManager.StorageType storageType)
        {
            string folderPath = Path.Combine(_baseStoragePath, storageType.ToString(), relativePath);

            string directoryPath = Path.GetDirectoryName(folderPath)!;

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            await File.WriteAllTextAsync(folderPath, xmlContent, new UTF8Encoding(false));

            return folderPath;
        }

        public async Task<string> SaveACECFAsync(string xmlContent, string rnc, string fileName)
        {
            string folderPath = Path.Combine(_baseStoragePath, "Certificacion", "ACECF", rnc);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fullPath = Path.Combine(folderPath, fileName);

            await File.WriteAllTextAsync(fullPath, xmlContent, new UTF8Encoding(false));

            return fullPath;
        }

    }
}