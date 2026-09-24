namespace MvcApp.Services;

public static class EmailTemplates
{
    public static bool LooksLikeHtml(string content)
    {
        return content.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || content.Contains("</", StringComparison.OrdinalIgnoreCase);
    }

    public static string ToHtml(string message)
    {
        return LooksLikeHtml(message)
            ? message
            : System.Net.WebUtility.HtmlEncode(message).Replace("\n", "<br />").Replace("\r", "");
    }

    public static string Build(string siteName, string subject, string message, string? logoUrl = null, string? tagline = null, string? heading = null, string? ctaText = null, string? ctaUrl = null)
    {
        var content = ToHtml(message);
        var logo = string.IsNullOrWhiteSpace(logoUrl) ? string.Empty :
            $"<img src=\"{System.Net.WebUtility.HtmlEncode(logoUrl)}\" alt=\"{System.Net.WebUtility.HtmlEncode(siteName)}\" style=\"max-height:28px;\" />&nbsp;&nbsp;";
        var taglineHtml = string.IsNullOrWhiteSpace(tagline)
            ? string.Empty
            : $"<div style=\"color:#9ca3af;font-size:13px;font-weight:400;margin-top:4px;\">{System.Net.WebUtility.HtmlEncode(tagline)}</div>";
        var headingHtml = string.IsNullOrWhiteSpace(heading)
            ? string.Empty
            : $"<h1 style=\"margin:0 0 16px;font-size:22px;font-weight:700;color:#111827;\">{System.Net.WebUtility.HtmlEncode(heading)}</h1>";
        var ctaHtml = string.IsNullOrWhiteSpace(ctaText) || string.IsNullOrWhiteSpace(ctaUrl)
            ? string.Empty
            : $"<div style=\"margin-top:24px;\"><a href=\"{System.Net.WebUtility.HtmlEncode(ctaUrl)}\" style=\"display:inline-block;padding:12px 28px;background-color:#2563eb;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600;\">{System.Net.WebUtility.HtmlEncode(ctaText)}</a></div>";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<title>{System.Net.WebUtility.HtmlEncode(subject)}</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f5;font-family:'Segoe UI',Arial,Helvetica,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f5;padding:32px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:560px;background-color:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb;"">
          <tr>
            <td style=""background-color:#111827;padding:20px 28px;color:#ffffff;"">
              <span style=""font-size:18px;font-weight:700;"">{logo}{System.Net.WebUtility.HtmlEncode(siteName)}</span>
              {taglineHtml}
            </td>
          </tr>
          <tr>
            <td style=""padding:28px;color:#1f2937;font-size:15px;line-height:1.6;"">
              {headingHtml}
              {content}
              {ctaHtml}
            </td>
          </tr>
          <tr>
            <td style=""padding:18px 28px;border-top:1px solid #e5e7eb;color:#6b7280;font-size:12px;line-height:1.5;text-align:center;"">
              &copy; {DateTime.Now.Year} {System.Net.WebUtility.HtmlEncode(siteName)} &mdash; This is an automated message. Please do not reply to this email.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
