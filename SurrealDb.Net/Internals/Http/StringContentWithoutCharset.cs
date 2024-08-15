using System.Text;

namespace SurrealDb.Net.Internals.Http;

internal class StringContentWithoutCharset : StringContent
{
    public StringContentWithoutCharset(string content)
        : base(content) { }

    public StringContentWithoutCharset(string content, Encoding encoding)
        : base(content, encoding)
    {
        ResetCharSet();
    }

    public StringContentWithoutCharset(string content, Encoding encoding, string mediaType)
        : base(content, encoding, mediaType)
    {
        ResetCharSet();
    }

    public StringContentWithoutCharset(string content, string mediaType)
        : base(content, Encoding.UTF8, mediaType)
    {
        ResetCharSet();
    }

    private void ResetCharSet()
    {
        if (Headers.ContentType is not null)
        {
            Headers.ContentType.CharSet = null;
        }
    }
}
