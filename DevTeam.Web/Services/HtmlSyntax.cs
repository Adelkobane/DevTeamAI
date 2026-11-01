namespace DevTeam.Web.Services;

public enum HtmlSpanKind { Text, Name, Attr, Value, Comment, Punct }

public readonly record struct HtmlSpan(string Text, HtmlSpanKind Kind);

public readonly record struct HtmlLine(int Number, IReadOnlyList<HtmlSpan> Spans);

/// <summary>
/// Delar HTML i rader och token så att källkodsfliken kan färglägga den utan att tolka den som sidans egen markup.
/// </summary>
public static class HtmlSyntax
{
    public static string FileName(string? title)
    {
        var slug = new string((title ?? string.Empty).Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray()).Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        return (slug.Length == 0 ? "prototyp" : slug) + ".html";
    }

    public static IReadOnlyList<HtmlLine> Highlight(string? html)
    {
        html = (html ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');

        var lines = new List<HtmlLine>();
        var spans = new List<HtmlSpan>();
        var number = 1;

        void FlushLine()
        {
            lines.Add(new HtmlLine(number++, spans));
            spans = [];
        }

        void Emit(HtmlSpanKind kind, string text)
        {
            var parts = text.Split('\n');
            for (var p = 0; p < parts.Length; p++)
            {
                if (p > 0) FlushLine();
                if (parts[p].Length > 0) spans.Add(new HtmlSpan(parts[p], kind));
            }
        }

        var i = 0;
        while (i < html.Length)
        {
            if (html.AsSpan(i).StartsWith("<!--"))
            {
                var end = html.IndexOf("-->", i + 4, StringComparison.Ordinal);
                var stop = end < 0 ? html.Length : end + 3;
                Emit(HtmlSpanKind.Comment, html[i..stop]);
                i = stop;
                continue;
            }

            if (html[i] == '<')
            {
                ReadTag(html, ref i, Emit);
                continue;
            }

            var next = html.IndexOf('<', i);
            if (next < 0) next = html.Length;
            Emit(HtmlSpanKind.Text, html[i..next]);
            i = next;
        }

        FlushLine();
        return lines;
    }

    private static void ReadTag(string html, ref int i, Action<HtmlSpanKind, string> emit)
    {
        var start = i;
        i++;
        if (i >= html.Length)
        {
            emit(HtmlSpanKind.Punct, "<");
            return;
        }

        if (html[i] == '!')
        {
            var end = html.IndexOf('>', i);
            var stop = end < 0 ? html.Length : end + 1;
            emit(HtmlSpanKind.Punct, html[start..stop]);
            i = stop;
            return;
        }

        if (html[i] == '/') i++;

        var nameStart = i;
        while (i < html.Length && IsName(html[i])) i++;
        emit(HtmlSpanKind.Punct, html[start..nameStart]);
        if (i > nameStart) emit(HtmlSpanKind.Name, html[nameStart..i]);

        while (i < html.Length)
        {
            if (html[i] == '>')
            {
                emit(HtmlSpanKind.Punct, ">");
                i++;
                return;
            }

            if (html[i] == '/' && i + 1 < html.Length && html[i + 1] == '>')
            {
                emit(HtmlSpanKind.Punct, "/>");
                i += 2;
                return;
            }

            if (html[i] is '"' or '\'')
            {
                i = EmitQuoted(html, i, emit);
                continue;
            }

            if (html[i] is '\n' or '\r')
            {
                emit(HtmlSpanKind.Punct, "\n");
                i++;
                continue;
            }

            if (char.IsWhiteSpace(html[i]))
            {
                var ws = i;
                while (i < html.Length && char.IsWhiteSpace(html[i]) && html[i] is not '\n' and not '\r') i++;
                emit(HtmlSpanKind.Punct, html[ws..i]);
                continue;
            }

            var attr = i;
            while (i < html.Length && html[i] is not '=' and not '>' and not '/' and not '"' and not '\'' && !char.IsWhiteSpace(html[i]))
                i++;
            if (i == attr)
            {
                emit(HtmlSpanKind.Punct, html[i].ToString());
                i++;
                continue;
            }

            emit(HtmlSpanKind.Attr, html[attr..i]);
            while (i < html.Length && char.IsWhiteSpace(html[i]) && html[i] is not '\n' and not '\r') i++;

            if (i >= html.Length || html[i] != '=') continue;

            emit(HtmlSpanKind.Punct, "=");
            i++;
            while (i < html.Length && char.IsWhiteSpace(html[i]) && html[i] is not '\n' and not '\r') i++;
            if (i >= html.Length) return;

            if (html[i] is '"' or '\'')
            {
                i = EmitQuoted(html, i, emit);
                continue;
            }

            var value = i;
            while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] != '>') i++;
            if (i > value) emit(HtmlSpanKind.Value, html[value..i]);
        }
    }

    private static int EmitQuoted(string html, int i, Action<HtmlSpanKind, string> emit)
    {
        var quote = html[i];
        var end = html.IndexOf(quote, i + 1);
        var stop = end < 0 ? html.Length : end + 1;
        emit(HtmlSpanKind.Value, html[i..stop]);
        return stop;
    }

    private static bool IsName(char c) => char.IsLetterOrDigit(c) || c is '-' or ':' or '_';
}
