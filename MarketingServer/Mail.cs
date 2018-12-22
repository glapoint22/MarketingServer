using System.Web;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Configuration;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MarketingServer
{
    public class Mail
    {
        private SmtpClient smtpClient = new SmtpClient();
        public string from;
        public string to;
        public string subject;
        public string body;
        
        public Mail(string emailId, Customer customer, string subject, string body)
        {
            // Get the mail settings from web.config
            Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration(HttpRuntime.AppDomainAppVirtualPath);
            MailSettingsSectionGroup mailSettings = configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;

            // Get the domain name
            //string domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority;
            string domain = "http://www.nicheshack.com";

            // Remove summary from the body
            body = Regex.Replace(body, @"summary=""[a-zA-Z0-9-.]+""", "");

            // Remove title from the body
            body = Regex.Replace(body, @"title=""[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)""", "");

            // Replace localhost with the domain name
            body = Regex.Replace(body, @"http://localhost(?::[0-9]+)?", domain);

            // Set the email properties
            this.subject = subject;
            this.body = string.Format(body, customer.Name, emailId, customer.ID, domain);
            to = customer.Email;
            from = mailSettings.Smtp.Network.UserName;
        }

        public async Task Send()
        {
            MailMessage mailMessage = new MailMessage(from, to, subject, body);
            mailMessage.IsBodyHtml = true;
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}