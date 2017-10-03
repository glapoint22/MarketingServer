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

            //See if we have an existing customer
            string id = await db.Customers.Where(c => c.Email == lead.email).Select(c => c.ID).FirstOrDefaultAsync();


            //If the customer DOES NOT exist in the database
            if (id == null)
            {
                customer = new Customer()
                {
                    ID = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                    Email = lead.email,
                    Name = lead.name,
                    EmailSendFrequency = 3,
                    EmailSentDate = DateTime.Today
                    
                };

                //Add the new customer to the database
                db.Customers.Add(customer);
            }
            else
            {
                customer = await db.Customers.FindAsync(id);
            }

            //Check to see if this customer is subscribed to this niche
            Subscription subscription = await db.Subscriptions.Where(x => x.CustomerID == customer.ID && x.NicheID == lead.nicheId).FirstOrDefaultAsync();
            if (subscription == null)
            {
                //Get a new subscription
                subscription = CreateSubscription(customer.ID, lead.nicheId);
                db.Subscriptions.Add(subscription);
                db.CampaignRecords.Add(await Campaign.CreateCampaignRecord(subscription.ID, subscription.NicheID));
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
            var email = await db.LeadMagnetEmails.Where(x => x.NicheID == lead.nicheId).Select(x => new
            {
                id = x.ID,
                subject = x.Subject,
                body = x.Body
            }
            ).SingleAsync();

            Mail mail = new Mail(email.id, customer, email.subject, email.body);
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
                    subscription = CreateSubscription(customer.ID, updatedSubscription.nicheId);
                    db.Subscriptions.Add(subscription);
                    db.CampaignRecords.Add(await Campaign.CreateCampaignRecord(subscription.ID, subscription.NicheID));
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

        public static Subscription CreateSubscription(string customerId, int nicheId)
        {
            Subscription subscription = new Subscription()
            {
                ID = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                CustomerID = customerId,
                NicheID = nicheId,
                Subscribed = true,
                Suspended = false,
                DateSubscribed = DateTime.Today
            };
            return subscription;
        }

        private async Task<object> GetSubscriptions(string customerId)
        {
            return await db.Categories.Select(c => new
            {
                name = c.Name,
                niches = db.Niches.Where(n => n.CategoryID == c.ID).Select(n => new
                {
                    id = n.ID,
                    name = n.Name,
                    isSubscribed = n.Subscriptions.Any(x => x.CustomerID == customerId && x.Subscribed && x.NicheID == n.ID),
                    subscriptionId = n.Subscriptions.Where(x => x.CustomerID == customerId && x.NicheID == n.ID).Select(x => x.ID).FirstOrDefault()
                }).ToList(),
                count = db.Niches.Where(n => n.CategoryID == c.ID).Count()
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
    public int nicheId;
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
    public int nicheId;
}
