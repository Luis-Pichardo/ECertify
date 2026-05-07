using System.Text.Json.Serialization;

namespace eCertify.Models
{
    public class HistorialPago
    {
        public long ID { get; set; }
        public long UserId { get; set; }
        public string PayPalOrderId { get; set; }
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string? PayerEmail { get; set; }
        public string? PayerName { get; set; }
        public DateTime PaymentDate { get; set; }

        // Relaciones
        [JsonIgnore]
        public User? User { get; set; }

    }
}
