namespace RSS_Reader.Services;

public class JsonFeedFormatException : Exception
{
    public JsonFeedFormatException(string message)
        : base(message) { }
}
