using System.Web;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Configuration;
using System.Net.Mail;
using System.Threading.Tasks;

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
            Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration(HttpRuntime.AppDomainAppVirtualPath);
            MailSettingsSectionGroup mailSettings = configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;

            this.subject = subject;
            this.body = string.Format(body, customer.Name, emailId, customer.ID);

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