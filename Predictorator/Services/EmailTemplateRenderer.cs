using System;

namespace Predictorator.Services;

public class EmailTemplateRenderer
{
    public string Render(string messageHtml, string baseUrl, string? unsubscribeToken, string? buttonText = null, string? buttonUrl = null, string? preheader = null)
    {
        var year = DateTime.UtcNow.Year;
        var buttonSection = string.Empty;
        if (!string.IsNullOrWhiteSpace(buttonText) && !string.IsNullOrWhiteSpace(buttonUrl))
        {
            buttonSection = $@"
              <p style=""text-align:center; margin:40px 0;"">
                <a href=""{buttonUrl}""
                   style=""
                     background-color:#0000ff;
                     color:#ffffff;
                     text-decoration:none;
                     padding:14px 28px;
                     border-radius:4px;
                     font-weight:bold;
                     font-size:16px;
                     text-transform:uppercase;
                     display:inline-block;
                   "">
                  {buttonText}
                </a>
              </p>";
        }

        var unsubscribeSection = string.Empty;
        if (!string.IsNullOrWhiteSpace(unsubscribeToken))
        {
            var link = $"{baseUrl}/Subscription/Unsubscribe?token={unsubscribeToken}";
            unsubscribeSection = $"<a href=\"{link}\" style=\"color:#555555; text-decoration:none; font-size:11px;\">Unsubscribe</a>";
        }

        var preheaderText = string.IsNullOrWhiteSpace(preheader)
            ? "Updates from Predictotronix"
            : preheader;

        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""UTF-8"">
  <title>Predictotronix</title>
  <style>
    .preheader {{ display:none !important; visibility:hidden; opacity:0; color:transparent; height:0; width:0; }}
  </style>
</head>
<body style=""margin:0; padding:0; background-color:#0a0a0a; color:#f0f0f0; font-family:'Courier New', Courier, monospace;"">
  <span class=""preheader"">
    {preheaderText}
  </span>
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#0a0a0a; padding:20px 0;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#1a1a1a; border:2px solid #00ff00; border-radius:8px; overflow:hidden;"">
          <tr>
            <td align=""center"" style=""background-color:#000000; padding:30px;"">
              <h1 style=""margin:0; font-size:32px; color:#0000ff; letter-spacing:2px;"">
                PREDICTOTRONIX
              </h1>
              <p style=""margin:5px 0 0 0; font-size:14px; color:#00ff00; text-transform:uppercase;"">
                Death to humans
              </p>
            </td>
          </tr>
          <tr>
            <td style=""padding:30px; font-size:16px; line-height:1.6; color:#00ffff;"">
              <p style=""margin-top:0;"">
                {messageHtml}
              </p>{buttonSection}
              <p style=""font-size:14px; color:#909090; text-align:center;"">
                You have 10 seconds to comply...
              </p>
            </td>
          </tr>
          <tr>
            <td style=""background-color:#000000; padding:15px 30px; text-align:center; font-size:12px; color:#555555;"">
              <p style=""margin:0;"">© {year} Predictotronix. All Rights Reserved.</p>
              <p style=""margin:8px 0 0 0;"">
                {unsubscribeSection}
              </p>
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
