namespace FileStorageDesign.Models
{
    public class CustomerDashboard
    {
        public Customer Customer { get; set; }
        public List<Order> Orders { get; set; }
        public List<PaymentTransaction> Payments { get; set; }
        public List<EmailRecord> Emails { get; set; }
    }
}