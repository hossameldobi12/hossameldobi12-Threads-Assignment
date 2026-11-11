namespace FileStorageDesign.Models
{
    public class EmailRecord
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string EmailAddress { get; set; }
        public int Attempts { get; set; }
        public DateTime SentAt { get; set; }
    }
}