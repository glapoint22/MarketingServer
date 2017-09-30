using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace MarketingServer.Controllers
{
    public class MailController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        public async Task<IHttpActionResult> GetMail(string emailId, string customerId)
        {
            string emailBody = await db.EmailCampaigns.Where(e => e.ID == emailId).Select(e => e.Body).FirstOrDefaultAsync();

            if (emailBody == null)
            {
                return BadRequest();
            }

            Mail mail = new Mail(emailId, await db.Customers.Where(c => c.ID == customerId).Select(c => c).SingleAsync(), "", emailBody);
            return Ok(mail.body);
        }
    }
}
