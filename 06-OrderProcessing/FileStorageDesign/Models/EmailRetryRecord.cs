namespace FileStorageDesign.Models
{
    public class EmailRetryRecord
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string EmailAddress { get; set; }
        public int Attempts { get; set; }
        public string LastError { get; set; }
        public DateTime FailedAt { get; set; }
    }
}