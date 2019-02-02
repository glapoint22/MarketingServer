using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using MarketingServer;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;

namespace MarketingServer.Controllers
{
    public class SubscriptionsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        [AllowAnonymous]
        public async Task<IHttpActionResult> Get()
        {
            string sessionId;
            Customer customer = null;

            sessionId = Session.GetSessionID(Request.Headers);

            if(sessionId != null) customer = await db.Customers.Where(x => x.SessionID == sessionId).FirstOrDefaultAsync();


            if (customer == null)
            {
                return Ok();
            }

            return Ok(await GetPreferences(customer));
        }

        [AllowAnonymous]
        public async Task<HttpResponseMessage> Post(SubscriptionInfo subscriptionInfo)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            bool isExistingCustomer = false;

            //See if we have an existing customer
            string id = await db.Customers.AsNoTracking().Where(c => c.Email == subscriptionInfo.email).Select(c => c.ID).FirstOrDefaultAsync();
            if (id != null) isExistingCustomer = true;

            string sessionId = Guid.NewGuid().ToString("N");

            //Set the customer
            Customer customer = await SetCustomer(id, subscriptionInfo.name, subscriptionInfo.email, sessionId);

            if(subscriptionInfo.leadMagnet != null)
            {
                //Check to see if this customer is subscribed to this niche
                Subscription subscription = await db.Subscriptions.AsNoTracking().Where(x => x.CustomerID == customer.ID && x.NicheID == subscriptionInfo.nicheId).FirstOrDefaultAsync();
                if (subscription == null)
                {
                    //Get a new subscription
                    subscription = CreateSubscription(customer.ID, subscriptionInfo.nicheId);
                    db.Subscriptions.Add(subscription);
                    db.CampaignRecords.Add(await Campaign.CreateCampaignRecord(subscription));
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
                var email = await db.LeadMagnetEmails.AsNoTracking().Where(x => x.NicheID == subscriptionInfo.nicheId).Select(x => new
                {
                    id = x.ID,
                    subject = x.Subject,
                    body = x.Body
                }
                ).SingleAsync();

                Mail mail = new Mail(email.id, customer, email.subject, email.body, await Mail.GetRelatedProducts(subscriptionInfo.nicheId, email.id, customer.ID, string.Empty));
                await mail.Send();

                //response = new
                //{
                //    leadMagnet = subscriptionInfo.leadMagnet,
                //    customer = new
                //    {
                //        id = customer.ID,
                //        email = customer.Email,
                //        name = customer.Name,
                //    },
                // };

                response.Content = new ObjectContent<object>(new
                {
                    leadMagnet = subscriptionInfo.leadMagnet,
                    customer = new
                    {
                        //id = customer.ID,
                        email = customer.Email,
                        name = customer.Name,
                    },
                }, new JsonMediaTypeFormatter());
            }
            else
            {
                //response = new
                //{
                //    customer = new
                //    {
                //        id = customer.ID,
                //        name = customer.Name,
                //        isExistingCustomer = isExistingCustomer
                //    }
                //};

                response.Content = new ObjectContent<object>(new
                {
                    customer = new
                    {
                        //id = customer.ID,
                        name = customer.Name,
                        isExistingCustomer = isExistingCustomer
                    }
                }, new JsonMediaTypeFormatter());

            }


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

            Session.SetSessionID(sessionId, Request, ref response);

            //CookieHeaderValue cookie = new CookieHeaderValue("session", sessionId);
            //cookie.Expires = DateTimeOffset.Now.AddYears(1);
            //cookie.Domain = Request.RequestUri.Host;
            //cookie.Path = "/";
            //response.Headers.AddCookies(new CookieHeaderValue[] { cookie });



            return response;

        }

        [AllowAnonymous]
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

                //Update the existing subscription
                subscription = await db.Subscriptions.FindAsync(updatedSubscription.subscriptionId);
                subscription.Subscribed = !subscription.Subscribed;
                if (!subscription.Subscribed) subscription.DateUnsubscribed = DateTime.Today;
                db.Entry(subscription).State = EntityState.Modified;
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [Route("api/Subscriptions/V2")]
        public async Task<IHttpActionResult> PutSubscriptions(Subscription subscription)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Entry(subscription).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(HttpStatusCode.InternalServerError);
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

        private async Task<Customer> SetCustomer(string id, string name, string email, string sessionId)
        {
            Customer customer;

            //If the customer DOES NOT exist in the database
            if (id == null)
            {
                customer = new Customer()
                {
                    ID = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                    Email = email,
                    Name = name,
                    EmailSendFrequency = 3,
                    EmailSentDate = DateTime.Today,
                    SessionID = Hashing.GetHash(sessionId)
                };

                //Add the new customer to the database
                db.Customers.Add(customer);
            }
            else
            {
                customer = await db.Customers.FindAsync(id);
                customer.SessionID = Hashing.GetHash(sessionId);
            }

            return customer;
        }

        private async Task<object> GetSubscriptions(string customerId)
        {
            return await db.Categories
                .AsNoTracking()
                .Where(n => db.Niches
                    .Where(s => db.Subscriptions
                        .Where(a => a.CustomerID == customerId)
                        .Select(a => a.NicheID)
                        .ToList()
                        .Contains(s.ID))
                    .Select(z => z.CategoryID)
                    .ToList()
                    .Contains(n.ID))
                .Select(x => new {
                    name = x.Name,
                    niches = db.Niches
                        .Where(y => y.CategoryID == x.ID && db.Subscriptions
                            .Where(a => a.CustomerID == customerId)
                            .Select(a => a.NicheID)
                            .ToList()
                            .Contains(y.ID))
                        .Select(n => new {
                            id = n.ID,
                            name = n.Name,
                            isSubscribed = n.Subscriptions.Any(a => a.CustomerID == customerId && a.Subscribed && a.NicheID == n.ID),
                            subscriptionId = n.Subscriptions.Where(a => a.CustomerID == customerId && a.NicheID == n.ID).Select(a => a.ID).FirstOrDefault()
                        }).ToList()
                })
                .ToListAsync();
        }

        private async Task<object> GetPreferences(Customer customer)
        {
            return new
            {
                customer = new
                {
                    //id = customer.ID,
                    email = customer.Email,
                    name = customer.Name,
                    emailSendFrequency = customer.EmailSendFrequency,
                    emailSentDate = customer.EmailSentDate
                },
                subscriptions = await GetSubscriptions(customer.ID)
            };
        }
    }
}

public struct SubscriptionInfo
{
    public string name;
    public string email;
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
    public string subscriptionId;
    public bool isSubscribed;
    public int nicheId;
}
