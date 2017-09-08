using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MarketingServer.Controllers
{
    public class MailController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        public async Task<IHttpActionResult> GetMail(Guid emailId, Guid customerId)
        {
            string emailBody = await db.Emails.Where(e => e.ID == emailId).Select(e => e.Body).SingleAsync();
            Mail mail = new Mail(emailId, await db.Customers.Where(c => c.ID == customerId).Select(c => c).SingleAsync(), "", emailBody);
            return Ok(mail.body);
        }
    }
}
