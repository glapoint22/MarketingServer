using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web.Http;

namespace MarketingServer.Controllers
{
    public class ContactController : ApiController
    {
        public IHttpActionResult Post(Contact contact)
        {
            SmtpClient smtpClient = new SmtpClient();
            MailMessage mailMessage = new MailMessage(contact.email, "glapoint22@gmail.com", contact.subject, contact.message);
            smtpClient.Send(mailMessage);

            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}

public struct Contact
{
    public string name;
    public string email;
    public string subject;
    public string message;
}
