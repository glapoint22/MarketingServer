using System.Web;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Configuration;
using System.Net.Mail;

namespace MarketingServer
{
    public class Mail
    {
        private MailSettingsSectionGroup mailSettings;
        private SmtpClient smtpClient = new SmtpClient();

        public Mail()
        {
            Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration(HttpRuntime.AppDomainAppVirtualPath);
            mailSettings = configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;
        }

        public void Send(string to, string subject, string body)
        {
            MailMessage mailMessage = new MailMessage(mailSettings.Smtp.Network.UserName, to, subject, body);
            mailMessage.IsBodyHtml = true;
            smtpClient.Send(mailMessage);
        }
    }
}