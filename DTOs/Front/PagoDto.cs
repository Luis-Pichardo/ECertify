namespace eCertify.DTOs.Front
{
    public class PagoDto
    {
        public string PayPalOrderId { get; set; }
        public string TransactionId { get; set; }
        public string PayerName { get; set; }
        public string PayerEmail { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int Quantity { get; set; }
    }
}
