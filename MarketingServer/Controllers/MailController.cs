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
        private static MarketingEntities db = new MarketingEntities();

        public async Task<IHttpActionResult> GetMail()
        {
            var categories = await db.Categories
                .AsNoTracking()
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
                            leadPages = z.LeadPages
                                .Where(a => a.NicheID == z.ID)
                                .Select(a => new {
                                    id = a.ID,
                                    title = a.Title,
                                    body = a.Body,
                                    pageTitle = a.PageTitle,
                                    leadMagnet = a.LeadMagnet,
                                    nicheId = z.ID
                                })
                                .ToList(),
                            emails = z.LeadMagnetEmails
                                .Where(a => a.NicheID == z.ID)
                                .Select(a => new
                                {
                                    id = a.ID,
                                    title = a.Subject,
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
                                        .Select(a => new
                                        {
                                            id = a.ID,
                                            title = a.Subject,
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
            var email = await db.EmailCampaigns.Where(e => e.ID == emailId).Select(e => new {
                nicheId = e.Product.NicheID,
                body = e.Body,
                productId = e.ProductID
            })
            .FirstOrDefaultAsync();

            

            if (email == null)
            {
                email = await db.LeadMagnetEmails.Where(e => e.ID == emailId).Select(e => new
                {
                    nicheId = e.NicheID,
                    body = e.Body,
                    productId = string.Empty
                })
                .FirstOrDefaultAsync();

                if (email == null) return BadRequest();
            }

            Mail mail = new Mail(emailId, await db.Customers.Where(c => c.ID == customerId).Select(c => c).SingleAsync(), "", email.body, await GetRelatedProducts(email.nicheId, emailId, customerId, email.productId));
            return Ok(mail.body);
        }

        public static async Task<List<Product>> GetRelatedProducts(int nicheId, string emailId, string customerId, string productId)
        {
            return await db.Products.Where(z => z.NicheID == nicheId && z.ID != productId && !z.CampaignRecords
                            .Where(a => a.SubscriptionID == db.Subscriptions.Where(x => x.CustomerID == customerId && x.Subscribed && x.NicheID == nicheId && !x.Suspended).Select(x => x.ID).FirstOrDefault() && a.ProductPurchased)
                            .Select(a => a.ProductID)
                            .ToList()
                            .Contains(z.ID))
                            .OrderBy(z => z.Name)
                            .ToListAsync();
        }
    }
}
