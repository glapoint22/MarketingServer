using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using MarketingServer;

namespace MarketingServer.Controllers
{
    public class SubscriptionsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        public async Task<IHttpActionResult> Get(Guid customerId)
        {
            Customer customer = await db.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound();
            }

            return Ok(await GetPreferences(customer));
        }

        public async Task<IHttpActionResult> Post(Leads lead)
        {
            Customer customer;

            //Get the sub niche based on the lead ID that was passed in
            var subNiche = await db.SubNiches.Where(x => x.LeadID == lead.leadId).Select(x => new
            {
                subNicheId = x.ID,
                leadMagnetEmailId = db.Emails.Where(e => e.CampaignID == db.Campaigns.Where(c => c.SubNicheID == x.ID).OrderBy(c => c.ID).Select(c => c.ID).FirstOrDefault() && e.Day == 0).Select(e => e.ID).FirstOrDefault()
            }
            ).SingleAsync();

            //See if we have an existing customer
            Guid id = await db.Customers.Where(c => c.Email == lead.email).Select(c => c.ID).FirstOrDefaultAsync();


            //If the customer DOES NOT exist in the database
            if (id == Guid.Empty)
            {
                customer = new Customer()
                {
                    ID = Guid.NewGuid(),
                    Email = lead.email,
                    Name = lead.name,
                    EmailSendFrequency = 3
                };

                //Add the new customer to the database
                db.Customers.Add(customer);
            }
            else
            {
                customer = await db.Customers.FindAsync(id);
            }

            //Check to see if this customer is subscribed to this sub niche
            Subscription subscription = await db.Subscriptions.Where(x => x.CustomerID == customer.ID && x.SubNicheID == subNiche.subNicheId).Select(x => x).FirstOrDefaultAsync();
            if (subscription == null)
            {
                //Get a new subscription
                subscription = await CreateSubscription(customer.ID, subNiche.subNicheId);
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
                    if (customer.EmailSendFrequency == 0) customer.EmailSendFrequency = 3;
                }
            }


            //Get the email and send
            var email = await db.Emails.Where(x => x.ID == subNiche.leadMagnetEmailId).Select(x => new
            {
                subject = x.Subject,
                body = x.Body
            }
            ).SingleAsync();

            Mail mail = new Mail(subNiche.leadMagnetEmailId, customer, email.subject, email.body);
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


            var response = new
            {
                leadMagnet = lead.leadMagnet,
                preferences = await GetPreferences(customer)
            };


            return Ok(response);
        }

        public async Task<IHttpActionResult> Put(Preferences preferences)
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
                        SubNicheID = updatedSubscription.subNicheId,
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

        private async Task<Subscription> CreateSubscription(Guid id, int subNicheId)
        {
            int currentCampaignID = await db.Campaigns.Where(x => x.SubNicheID == subNicheId).Select(x => x.ID).FirstOrDefaultAsync();

            Subscription subscription = new Subscription()
            {
                CustomerID = id,
                SubNicheID = subNicheId,
                Subscribed = true,
                Suspended = false,
                DateSubscribed = DateTime.Today
            };

            return subscription;
        }

        private async Task<object> GetSubscriptions(Guid customerId)
        {
            return await db.Niches.Select(c => new
            {
                name = c.Name,
                subNiches = db.SubNiches.Where(n => n.ParentNicheID == c.ID).Select(n => new
                {
                    id = n.ID,
                    name = n.Name,
                    isSubscribed = n.Subscriptions.Any(x => x.CustomerID == customerId && x.Subscribed && x.SubNicheID == n.ID),
                    subscriptionId = n.Subscriptions.Where(x => x.CustomerID == customerId && x.SubNicheID == n.ID).Select(x => x.ID).FirstOrDefault()
                }).ToList(),
                count = db.SubNiches.Where(n => n.ParentNicheID == c.ID).Count()
            }).OrderByDescending(x => x.count).ToListAsync();
        }

        private async Task<object> GetPreferences(Customer customer)
        {
            return new
            {
                customer = new
                {
                    id = customer.ID,
                    email = customer.Email,
                    name = customer.Name,
                    emailSendFrequency = customer.EmailSendFrequency
                },
                subscriptions = await GetSubscriptions(customer.ID)
            };
        }
    }
}

public struct Leads
{
    public string email;
    public string name;
    public int leadId;
    public string leadMagnet;
}
public struct Preferences
{
    public bool customerModified;
    public Customer customer;
    public UpdatedSubscription[] updatedSubscriptions;

}
public struct UpdatedSubscription
{
    public int subscriptionId;
    public bool isSubscribed;
    public int subNicheId;
}
