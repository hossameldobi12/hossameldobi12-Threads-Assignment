namespace FileStorageDesign.Exceptions
{
    [Serializable]
    internal class DatabaseWriteException : Exception
    {
        public DatabaseWriteException()
        {
        }

        public DatabaseWriteException(string? message) : base(message)
        {
        }

        public DatabaseWriteException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}