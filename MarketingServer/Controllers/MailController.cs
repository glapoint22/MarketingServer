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

        public async Task<IHttpActionResult> GetMail()
        {
            var categories = await db.Categories
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    id = x.ID,
                    name = x.Name,
                    niches = x.Niches
                        .Select(z => new
                        {
                            id = z.ID,
                            name = z.Name,
                            emails = z.LeadMagnetEmails
                                .Where(a => a.NicheID == z.ID)
                                .Select(a => new {
                                    id = a.ID,
                                    subject = a.Subject,
                                    body = a.Body
                                })
                                .ToList(),
                            products = z.Products
                                .Select(p => new
                                {
                                    id = p.ID,
                                    name = p.Name,
                                    emails = p.EmailCampaigns
                                        .OrderBy(a => a.Day)
                                        .Where(a => a.ProductID == p.ID)
                                        .Select(a => new {
                                            id = a.ID,
                                            day = a.Day,
                                            subject = a.Subject,
                                            body = a.Body
                                        })
                                        .ToList()
                                })
                                .ToList()
                        }).ToList()
                }
            )
            .ToListAsync();

            return Ok(categories);
        }



        public async Task<IHttpActionResult> GetMail(string emailId, string customerId)
        {
            string emailBody = await db.EmailCampaigns.Where(e => e.ID == emailId).Select(e => e.Body).FirstOrDefaultAsync();

            if (emailBody == null)
            {
                emailBody = await db.LeadMagnetEmails.Where(e => e.ID == emailId).Select(e => e.Body).FirstOrDefaultAsync();

                if(emailBody == null) return BadRequest();
            }

            Mail mail = new Mail(emailId, await db.Customers.Where(c => c.ID == customerId).Select(c => c).SingleAsync(), "", emailBody);
            return Ok(mail.body);
        }
    }
}
