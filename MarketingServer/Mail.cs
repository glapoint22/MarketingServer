using System.Web;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Configuration;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace MarketingServer
{
    public class Mail
    {
        private SmtpClient smtpClient = new SmtpClient();
        public string from;
        public string to;
        public string subject;
        public string body;
        
        public Mail(string emailId, Customer customer, string subject, string body, List<Product> products)
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

            // Get the email footer
            string filePath = HttpContext.Current.Server.MapPath("~/EmailFooter.txt");
            StreamReader file = new StreamReader(filePath);

            // Get the footer from the file
            string footer = "";
            string line;

            while((line = file.ReadLine()) != null)
            {
                footer += line;
            }

            file.Close();

            body = Regex.Replace(body, @"<\/table><!--\[if \(gte mso 9\)\|\(IE\)\]><\/td><\/tr><\/table><!\[endif\]--><\/td><\/tr><\/table>$", AddProducts(products));

            // Add the footer to the body
            body = Regex.Replace(body, @"<\/table><!--\[if \(gte mso 9\)\|\(IE\)\]><\/td><\/tr><\/table><!\[endif\]--><\/td><\/tr><\/table>$", footer);

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

        public string AddProducts(List<Product> products)
        {
            string caption = "<tr><td align=\"center\" valign=\"top\" style=\"padding-left: 0px; padding-right: 0px; padding-bottom: 0px; font-size: 0px;\"><!--[if (gte mso 9)|(IE)]><table width=\"599.9999828338623\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#858585\"><tr><td><![endif]--><table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#858585\"  style=\"max-width: 600px;\"><tr><td height=\"8\"></td></tr><tr><td align=\"center\" valign=\"top\" style=\"padding-left: 0px; padding-right: 0px; padding-bottom: 0px; font-size: 0px;\"><!--[if (gte mso 9)|(IE)]><table width=\"599.9999732971519\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr><td><![endif]--><table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"  style=\"max-width: 600px;\"><tr><td style=\"text-align: center;\"><span style=\"color: rgb(214, 214, 214); font-size: 30px; font-family: &quot;Times New Roman&quot;, Times, serif; font-weight: bold; font-style: italic;\">Check out these similar products</span></td></tr></table><!--[if (gte mso 9)|(IE)]></td></tr></table><![endif]--></td></tr><tr><td height=\"8\"></td></tr></table><!--[if (gte mso 9)|(IE)]></td></tr></table><![endif]--></td></tr><tr><td align=\"center\" valign=\"top\" style=\"padding-left: 0px; padding-right: 0px; padding-bottom: 0px; font-size: 0px;\"><!--[if (gte mso 9)|(IE)]><table width=\"599.9999999994179\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#ededed\"><tr><td><![endif]--><table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#ededed\"  style=\"max-width: 600px;\"><tr><td height=\"10\"></td></tr>";
            return caption;
        }
    }
}