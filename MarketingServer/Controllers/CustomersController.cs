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
            //Get the niche based on the lead page that was passed in
            var niche = await db.Niches.Where(x => x.LeadPage == body.leadPage).Select(x => new
                {
                    nicheId = x.ID,
                    emailId = x.EmailID
                }
            ).SingleAsync();
            
            //Set the customer object
            Customer customer = new Customer()
            {
                ID = await db.Customers.Where(c => c.Email == body.email).Select(c => c.ID).FirstOrDefaultAsync(),
                Email = body.email,
                Name = body.name,
                EmailSentDate = DateTime.Today,
                EmailSendFrequency = 3
            };


            //If the customer DOES NOT exist in the database
            if (customer.ID == Guid.Empty)
            {
                customer.ID = Guid.NewGuid();
                //Add the new customer to the database
                db.Customers.Add(customer);
            }
            
            //Check to see if this customer is subscribed to this niche
            if (!IsSubscribed(customer.ID, niche.nicheId))
            {
                //Subscribe the customer to this niche
                Subscription subscription = await GetSubscription(customer.ID, niche.nicheId);
                db.Subscriptions.Add(subscription);
            }


            //Get the email and send
            var email = await db.Emails.Where(x => x.ID == niche.emailId).Select(x => new 
            {
                subject = x.Subject,
                body = x.Body
            }
            ).SingleAsync();

            Mail mail = new Mail(niche.emailId, customer, email.subject, email.body);

            mail.Send();


            //If there are any changes, update the database
            if (db.ChangeTracker.HasChanges())
            {
                try
                {
                    await db.SaveChangesAsync();
                    customer.Subscriptions = new Subscription[0];
                }
                catch (DbUpdateException)
                {
                    throw;
                }
            }
                

            return CreatedAtRoute("DefaultApi", new { id = customer.ID }, customer);
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
                NextEmailToSend = await db.Emails.Where(x => x.Day == 1 && x.CampaignID == db.Campaigns.Where(c => c.NicheID == nicheId).Select(c => c.ID).FirstOrDefault()).Select(x => x.ID).SingleAsync(),
                Active = true,
                Subscribed = true,
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