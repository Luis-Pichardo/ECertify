using eCertify.Models;

namespace eCertify.Interfaces
{
    public interface ICommercialApprovalService
    {
        Task<string> GenerateAndSignXmlAsync(ACECF model);
        (bool Success, string Message) ValidateApproval(string xmlContent);
    }
}
