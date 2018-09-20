using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace EmailService
{
    public class Mail
    {
        private SmtpClient smtpClient = new SmtpClient();
        public string from;
        public string to;
        public string subject;
        public string body;
        
        public Mail(string emailId, Customer customer, string productId, string subject, string body)
        {
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.Host = "smtp.gmail.com";
            smtpClient.Port = 587;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential("glapoint22@gmail.com", "Cyb668622");
            smtpClient.EnableSsl = true;

            this.subject = subject;
            this.body = string.Format(body, customer.Name, emailId, customer.ID, productId);

            to = customer.Email;
            from = "glapoint22@gmail.com";
        }

        public async Task Send()
        {
            MailMessage mailMessage = new MailMessage(from, to, subject, body);
            mailMessage.IsBodyHtml = true;
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}