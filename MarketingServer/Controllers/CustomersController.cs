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
        public async Task<IHttpActionResult> PutCustomer(Preferences preferences)
        {
            //Assign the customer
            Customer customer = preferences.customer;

            //If the customer info has been modified
            if (preferences.customerModified)
            {
                db.Entry(customer).State = EntityState.Modified;

                //If unsubscribing to all subscriptions
                if (customer.EmailSendFrequency == 0)
                {
                    await db.Subscriptions.Where(x => x.CustomerID == customer.ID).ForEachAsync(x => {
                        x.Subscribed = false;
                        x.DateUnsubscribed = DateTime.Today;
                    });
                }
            }
            
            //Iterate through the subscriptions
            foreach (UpdatedSubscription updatedSubscription in preferences.updatedSubscriptions)
            {
                Subscription subscription;

                //If subscribing to a new subscription
                if (updatedSubscription.subscriptionId == 0)
                {
                    subscription = new Subscription
                    {
                        CustomerID = customer.ID,
                        NicheID = updatedSubscription.nicheId,
                        Subscribed = updatedSubscription.isSubscribed,
                        DateSubscribed = DateTime.Today
                    };

                    db.Subscriptions.Add(subscription);
                }
                else
                {
                    //Update the existing subscription
                    subscription = await db.Subscriptions.FindAsync(updatedSubscription.subscriptionId);
                    subscription.Subscribed = !subscription.Subscribed;
                    if (!subscription.Subscribed) subscription.DateUnsubscribed = DateTime.Today;
                    db.Entry(subscription).State = EntityState.Modified;
                }
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
            
            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Customers
        [ResponseType(typeof(Customer))]
        public async Task<IHttpActionResult> PostCustomer(Lead body)
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
            Subscription subscription = await db.Subscriptions.Where(x => x.CustomerID == customer.ID && x.NicheID == niche.nicheId).Select(x => x).FirstOrDefaultAsync();
            if(subscription == null)
            {
                //Get a new subscription
                subscription = await GetSubscription(customer.ID, niche.nicheId);
                db.Subscriptions.Add(subscription);
            }
            else
            {
                if (!subscription.Subscribed)
                {
                    //Renew the subscription
                    subscription.Subscribed = true;
                    subscription.DateSubscribed = DateTime.Today;

                    //If the customer was unsubscribed to all, set the email send frequency to default
                    if(customer.EmailSendFrequency == 0) customer.EmailSendFrequency = 3;
                }
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
                        id = n.ID,
                        name = n.Name,
                        isSubscribed = n.Subscriptions.Any(x => x.CustomerID == customer.ID && x.Subscribed && x.NicheID == n.ID),
                        subscriptionId = n.Subscriptions.Where(x => x.CustomerID == customer.ID && x.NicheID == n.ID).Select(x => x.ID).FirstOrDefault()
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

        //private bool IsSubscribed(Guid id, int nicheId)
        //{
        //    return db.Subscriptions.Count(x => x.CustomerID == id && x.NicheID == nicheId) > 0;
        //}

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