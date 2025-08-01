using System.Net;
using System.Text.RegularExpressions;

namespace RSS_Reader.Helpers
{
    public static class HtmlUtils
    {
        public static string RemoveHtmlTags(string html)
        {
            html = Regex.Replace(html, "<(script|style|noscript|iframe|object|embed|applet|head|template|svg)[^>]*>.*?</\\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", string.Empty).Trim());
        }
    }
}