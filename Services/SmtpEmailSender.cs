using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using NetRoll.Data;
using NetRoll.Models;

namespace NetRoll.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly ApplicationDbContext _db;
    private readonly IStringLocalizer<SmtpEmailSender> _L;

    public SmtpEmailSender(ApplicationDbContext db, IStringLocalizer<SmtpEmailSender> localizer)
    {
        _db = db;
        _L = localizer;
    }

    public async Task SendConfirmationLinkAsync(string email, string confirmationLink)
        => await SendEmailAsync(email, _L["ConfirmEmailSubject"], string.Format(_L["ConfirmEmailBodyHtml"], confirmationLink));

    public async Task SendPasswordResetCodeAsync(string email, string resetCode)
        => await SendEmailAsync(email, _L["ResetPasswordSubject"], string.Format(_L["ResetPasswordCodeBodyHtml"], WebUtility.HtmlEncode(resetCode)));

    public async Task SendPasswordResetLinkAsync(string email, string resetLink)
        => await SendEmailAsync(email, _L["ResetPasswordSubject"], string.Format(_L["ResetPasswordLinkBodyHtml"], resetLink));

    public async Task SendTestEmailAsync(string toEmail)
        => await SendEmailAsync(toEmail, _L["TestEmailSubject"], _L["TestEmailBodyHtml"]);

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
        => SendEmailInternalAsync(email, subject, htmlMessage);

    private async Task SendEmailInternalAsync(string toEmail, string subject, string htmlBody)
    {
        var settings = await _db.EmailSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1);
    if (settings is null || string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            // No SMTP configured; silently no-op or throw. We'll no-op to avoid breaking flows.
            return;
        }

    using var client = new SmtpClient(settings.SmtpHost, settings.Port)
        {
            EnableSsl = settings.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        }

        var from = new MailAddress(settings.FromEmail ?? settings.Username ?? "no-reply@example.com", settings.FromName ?? "NetRoll");
        var to = new MailAddress(toEmail);

        using var message = new MailMessage(from, to)
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        await client.SendMailAsync(message);
    }
}
