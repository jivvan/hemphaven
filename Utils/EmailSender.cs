using Microsoft.AspNetCore.Identity.UI.Services;

namespace BulkyWeb.Utils
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string body)
        {
            // logic to send email
            return Task.CompletedTask;
        }
    }
}