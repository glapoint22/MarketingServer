using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

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
                                .Select(a => new
                                {
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

        [AllowAnonymous]
        public async Task<IHttpActionResult> GetMail(string emailId)
        {
            string sessionId;
            Customer customer = null;

            sessionId = Session.GetSessionID(Request.Headers);

            if (sessionId != null) customer = await db.Customers.AsNoTracking().Where(x => x.SessionID == sessionId).FirstOrDefaultAsync();

            if (customer != null)
            {
                // Search email campaigns for this email id
                var email = await db.EmailCampaigns
                    .AsNoTracking()
                    .Where(e => e.ID == emailId)
                    .Select(e => new
                    {
                        nicheId = e.Product.NicheID,
                        body = e.Body,
                        productId = e.ProductID
                    })
                    .FirstOrDefaultAsync();


                // If email was not in email campaigns, search in leadmagnet emails
                if (email == null)
                {
                    email = await db.LeadMagnetEmails
                        .AsNoTracking()
                        .Where(e => e.ID == emailId)
                        .Select(e => new
                        {
                            nicheId = e.NicheID,
                            body = e.Body,
                            productId = string.Empty
                        })
                    .FirstOrDefaultAsync();

                    // If not found, return bad request
                    if (email == null) return BadRequest();
                }

                // Make a new mail instance
                Mail mail = new Mail(emailId, customer, "", email.body, await Mail.GetRelatedProducts(email.nicheId, emailId, customer.ID, email.productId));
                return Ok(mail.body);
            }
            else
            {
                return Ok();
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> PostMail(CampaignEmail campaignEmail)
        {
            var email = await db.EmailCampaigns
                .AsNoTracking()
                .Where(x => x.ProductID == campaignEmail.productId && x.Day == campaignEmail.day)
                .Select(x => new
                {
                    id = x.ID,
                    subject = x.Subject,
                    body = x.Body,
                    nicheId = x.Product.NicheID
                })
                .FirstOrDefaultAsync();

            if (email == null) return NotFound();

            Mail mail = new Mail(email.id, campaignEmail.customer, email.subject, email.body, await Mail.GetRelatedProducts(email.nicheId, email.id, campaignEmail.customer.ID, campaignEmail.productId));
            await mail.Send();
            return Ok();
        }
    }
}