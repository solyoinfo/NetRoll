using System.Text.RegularExpressions;

namespace NetRoll.Services;

public interface IHtmlSanitizerService
{
    string Sanitize(string? html);
}

public class HtmlSanitizerService : IHtmlSanitizerService
{
    // Very small whitelist sanitizer (NOT for untrusted public input at scale)
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "b","strong","i","em","u","p","br","ul","ol","li","span","small","code","pre","div" ,"h1","h2","h3","h4","h5","h6","a"
    };
    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase){"href","class","target","rel"};

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        // Remove script/style blocks
    html = Regex.Replace(html, @"<\s*(script|style)[^>]*?>[\s\S]*?<\/\s*(script|style)>", string.Empty, RegexOptions.IgnoreCase);
        // Process tags
        html = Regex.Replace(html, "<([^>]+)>", m => FilterTag(m.Groups[1].Value));
        return html;
    }

    private string FilterTag(string inside)
    {
        var parts = inside.Trim().Split(new[]{' '},2,StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return string.Empty;
        var tag = parts[0];
        var isEnd = tag.StartsWith("/");
        var tagName = isEnd ? tag.TrimStart('/') : tag;
        if (!AllowedTags.Contains(tagName)) return string.Empty;
        if (isEnd) return $"</{tagName}>";
        var attrSegment = parts.Length>1 ? parts[1] : string.Empty;
        var attrs = new List<string>();
        foreach (Match am in Regex.Matches(attrSegment, "([a-zA-Z0-9:-]+)\\s*=\\s*\"([^\"]*)\""))
        {
            var an = am.Groups[1].Value;
            var av = am.Groups[2].Value;
            if (AllowedAttributes.Contains(an))
            {
                // Basic href safe check
                if (an.Equals("href", StringComparison.OrdinalIgnoreCase) && av.StartsWith("javascript", StringComparison.OrdinalIgnoreCase)) continue;
                attrs.Add($"{an}=\"{System.Net.WebUtility.HtmlEncode(av)}\"");
            }
        }
        var attrOut = attrs.Count>0 ? (" "+string.Join(' ', attrs)) : string.Empty;
        var selfClose = tagName.Equals("br", StringComparison.OrdinalIgnoreCase) ? "/" : string.Empty;
        return $"<{tagName}{attrOut}{selfClose}>";
    }
}
