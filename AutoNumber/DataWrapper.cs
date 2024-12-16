namespace AutoNumber
{
    public class DataWrapper
    {
        public DataWrapper(string value, Azure.ETag eTag)
        {
            Value = value;
            ETag = eTag;
        }

        public Azure.ETag ETag { get; private set; }

        public string Value { get; private set; }
    }
}