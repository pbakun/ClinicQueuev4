using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Helpers
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        //private readonly ILogger _logger;

        public EmailSender(IOptions<EmailSettings> emailSettings/*, ILogger logger*/)
        {
            _emailSettings = emailSettings.Value;
            //_logger = logger;
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
                    //_logger.LogInformation("Sending email to {0}", email);
                    await client.SendMailAsync(mail);
                }
            }
            catch(Exception ex)
            {
                //_logger.LogError(ex, "Error while sending email");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
