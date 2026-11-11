namespace FileStorageDesign.Exceptions
{
    [Serializable]
    internal class InvalidCouponException : Exception
    {
        public InvalidCouponException()
        {
        }

        public InvalidCouponException(string? message) : base(message)
        {
        }

        public InvalidCouponException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}