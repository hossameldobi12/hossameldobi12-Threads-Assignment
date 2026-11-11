namespace FileStorageDesign.Exceptions
{
    [Serializable]
    internal class SmtpUnavailableException : Exception
    {
        public SmtpUnavailableException()
        {
        }

        public SmtpUnavailableException(string? message) : base(message)
        {
        }

        public SmtpUnavailableException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}