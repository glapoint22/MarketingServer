using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

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
                            leadMagnet = z.LeadMagnet,
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
                                    hoplink = p.HopLink,
                                    emails = p.EmailCampaigns
                                        .OrderBy(a => a.Day)
                                        .Where(a => a.ProductID == p.ID)
                                        .Select(a => new {
                                            id = a.ID,
                                            subject = a.Subject,
                                            body = a.Body,
                                            day = a.Day
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


        // PUT: api/Products/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutProduct(Product[] products)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (Product product in products)
            {
                // Get a list of email campaigns for this product
                List<EmailCampaign> dbEmailCampaigns = await db.EmailCampaigns.Where(x => x.ProductID == product.ID).ToListAsync();

                // Check to see if any emails have been deleted
                foreach (EmailCampaign dbEmailCampaign in dbEmailCampaigns)
                {
                    if (!product.EmailCampaigns.Select(x => x.ID).ToList().Contains(dbEmailCampaign.ID))
                    {
                        db.EmailCampaigns.Remove(dbEmailCampaign);
                        
                    }
                }

                // Check to see if any email campaigns need to be added or have been modified
                foreach (EmailCampaign emailCampaign in product.EmailCampaigns)
                {
                    if (!(dbEmailCampaigns.Count(x => x.ID == emailCampaign.ID) > 0))
                    {
                        db.Entry(emailCampaign).State = EntityState.Added;
                    }
                    else
                    {
                        EmailCampaign dbEmailCampaign = dbEmailCampaigns.FirstOrDefault(x => x.ID == emailCampaign.ID);
                        db.Entry(dbEmailCampaign).State = EntityState.Detached;
                        if (dbEmailCampaign.Subject != emailCampaign.Subject || dbEmailCampaign.Body != emailCampaign.Body || dbEmailCampaign.Day != emailCampaign.Day)
                        {
                            db.Entry(emailCampaign).State = EntityState.Modified;
                        }
                    }
                }
            }

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
