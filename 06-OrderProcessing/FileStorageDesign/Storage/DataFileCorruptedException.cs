namespace OrderProcessingSystem.Storage
{
    [Serializable]
    internal class DataFileCorruptedException : Exception
    {
        public DataFileCorruptedException()
        {
        }

        public DataFileCorruptedException(string? message) : base(message)
        {
        }

        public DataFileCorruptedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}