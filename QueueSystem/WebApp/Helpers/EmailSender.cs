using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using WebApp.Models;
using WebApp.Utility;

namespace WebApp.Helpers
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var mail = new MailMessage()
                {
                    From = new MailAddress(_emailSettings.SenderMail),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                    BodyEncoding = System.Text.Encoding.UTF8
                };
                mail.To.Add(new MailAddress(email));

                using (var client = new SmtpClient(_emailSettings.MailServer, _emailSettings.Port))
                {
                    client.Credentials = new NetworkCredential(_emailSettings.SenderMail, _emailSettings.Password);
                    await client.SendMailAsync(mail);
                    Log.Information(LogMessage.EmailSent(email));
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}
