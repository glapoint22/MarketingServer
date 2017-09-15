using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using MarketingServer;
using System.Web;

namespace MarketingServer.Controllers
{
    public class CustomersController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Customers
        public IQueryable<Customer> GetCustomers()
        {
            return db.Customers;
        }

        // GET: api/Customers/5
        [ResponseType(typeof(Customer))]
        public async Task<IHttpActionResult> GetCustomer(Guid id)
        {
            Customer customer = await db.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }

        // PUT: api/Customers/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutCustomer(Guid id, Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != customer.ID)
            {
                return BadRequest();
            }

            db.Entry(customer).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Customers
        [ResponseType(typeof(Customer))]
        public async Task<IHttpActionResult> PostCustomer(Body body)
        {
            Customer customer;

            //Get the niche based on the lead page that was passed in
            var niche = await db.Niches.Where(x => x.LeadPage == body.leadPage).Select(x => new
                {
                    nicheId = x.ID,
                    emailId = db.Emails.Where(e => e.CampaignID == db.Campaigns.Where(c => c.NicheID == x.ID).OrderBy(c => c.ID).Select(c => c.ID).FirstOrDefault() && e.Day == 0).Select(e => e.ID).FirstOrDefault(),
                    leadMagnet = x.LeadMagnet
                }
            ).SingleAsync();

            //See if we have an existing customer
            Guid id = await db.Customers.Where(c => c.Email == body.email).Select(c => c.ID).FirstOrDefaultAsync();


            //If the customer DOES NOT exist in the database
            if (id == Guid.Empty)
            {
                customer = new Customer()
                {
                    ID = Guid.NewGuid(),
                    Email = body.email,
                    Name = body.name,
                    EmailSendFrequency = 3
                };

                //Add the new customer to the database
                db.Customers.Add(customer);
            }
            else
            {
                customer = await db.Customers.FindAsync(id);
            }


            
            //Check to see if this customer is subscribed to this niche
            if (!IsSubscribed(customer.ID, niche.nicheId))
            {
                //Subscribe the customer to this niche
                Subscription subscription = await GetSubscription(customer.ID, niche.nicheId);
                db.Subscriptions.Add(subscription);

                //Add a log for this new campaign
                CampaignLog campaignLog = new CampaignLog
                {
                    SubscriptionID = subscription.ID,
                    Date = DateTime.Today,
                    CampaignID = await db.Campaigns.Where(c => c.NicheID == niche.nicheId).OrderBy(c => c.ID).Select(c => c.ID).FirstOrDefaultAsync(),
                    Day = 0,
                    CustomerID = customer.ID
                };
                db.CampaignLogs.Add(campaignLog);
            }


            //Get the email and send
            var email = await db.Emails.Where(x => x.ID == niche.emailId).Select(x => new 
            {
                subject = x.Subject,
                body = x.Body
            }
            ).SingleAsync();

            Mail mail = new Mail(niche.emailId, customer, email.subject, email.body);
            //mail.Send();


            //If there are any changes, update the database
            if (db.ChangeTracker.HasChanges())
            {
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    throw;
                }
            }


            var data = new
            {
                leadMagnet = niche.leadMagnet,
                customer = new
                {
                    id = customer.ID,
                    email = customer.Email,
                    name = customer.Name,
                    emailSendFrequency = customer.EmailSendFrequency
                },
                subscriptions = await db.Categories.Select(c => new
                {
                    name = c.Name,
                    niches = db.Niches.Where(n => n.CategoryID == c.ID).Select(n => new {
                        name = n.Name,
                        isSubscribed = db.Subscriptions.Any(x => x.CustomerID == customer.ID && x.Subscribed && x.NicheID == n.ID)
                    }).ToList(),
                    count = db.Niches.Where(n => n.CategoryID == c.ID).Count()
                }).OrderByDescending(x => x.count).ToListAsync()
            };

                
            return CreatedAtRoute("DefaultApi", new { id = customer.ID }, data);
        }

        // DELETE: api/Customers/5
        [ResponseType(typeof(Customer))]
        public async Task<IHttpActionResult> DeleteCustomer(Guid id)
        {
            Customer customer = await db.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            db.Customers.Remove(customer);
            await db.SaveChangesAsync();

            return Ok(customer);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CustomerExists(Guid id)
        {
            return db.Customers.Count(e => e.ID == id) > 0;
        }

        private bool IsSubscribed(Guid id, int nicheId)
        {
            return db.Subscriptions.Count(x => x.CustomerID == id && x.NicheID == nicheId) > 0;
        }

        private async Task<Subscription> GetSubscription(Guid id, int nicheId)
        {
            int currentCampaignID = await db.Campaigns.Where(x => x.NicheID == nicheId).Select(x => x.ID).FirstOrDefaultAsync();

            Subscription subscription = new Subscription()
            {
                CustomerID = id,
                NicheID = nicheId,
                Subscribed = true,
                Suspended = false,
                DateSubscribed = DateTime.Today
            };

            return subscription;
        }
    }
}

public struct Body
{
    public string email;
    public string name;
    public string leadPage;
}