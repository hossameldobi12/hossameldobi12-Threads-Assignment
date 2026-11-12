namespace FileStorageDesign.Exceptions
{
    [Serializable]
    public class ShippingProviderUnavailableException : Exception
    {
        public ShippingProviderUnavailableException()
        {
        }

        public ShippingProviderUnavailableException(string? message) : base(message)
        {
        }

        public ShippingProviderUnavailableException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}