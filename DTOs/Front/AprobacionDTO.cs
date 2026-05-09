using System.Text.Json.Serialization;

namespace eCertify.DTOs.Front
{
    public class CommercialApprovalRowDto
    {
        [JsonPropertyName("rowNumber")]
        public int RowNumber { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("issuerRnc")]
        public string? IssuerRnc { get; set; }

        [JsonPropertyName("encf")]
        public string? Encf { get; set; }

        [JsonPropertyName("issueDate")]
        public string? IssueDate { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("buyerRnc")]
        public string? BuyerRnc { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("rejectionDetail")]
        public string? RejectionDetail { get; set; }

        [JsonPropertyName("commercialApprovalDateTime")]
        public string? CommercialApprovalDateTime { get; set; }
    }
}
