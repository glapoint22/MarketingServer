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
        public async Task<IHttpActionResult> GetCustomer(string id)
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
        public async Task<IHttpActionResult> PutCustomer(string id, Customer customer)
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
            //Get the niche ID based on the lead page that was passed in
            int nicheID = await db.Niches.Where(x => x.LeadPage == body.leadPage).Select(x => x.ID).SingleAsync();
            Mail mail = new Mail();

            //Set the customer object
            Customer customer = new Customer()
            {
                ID = body.email,
                Name = body.name,
                EmailSentDate = DateTime.Today,
                EmailSendFrequency = 3
            };


            //If the customer DOES NOT exist in the database
            if (!CustomerExists(customer.ID))
            {
                //Add the new customer to the database
                db.Customers.Add(customer);
            }
            else //This customer DOES exist in the database
            {
                //If this customer is already subscribed to this niche
                if (IsSubscribed(customer.ID, nicheID))
                {
                    return StatusCode(HttpStatusCode.NoContent);
                }
            }

            //Subscribe the customer to this niche
            Subscription subscription = await GetSubscription(customer.ID, nicheID);
            db.Subscriptions.Add(subscription);

            mail.Send(customer.ID, "Test", "This is a test!");



            try
            {
                await db.SaveChangesAsync();
                customer.Subscriptions = new Subscription[0];
            }
            catch (DbUpdateException)
            {
                throw;
            }

            return CreatedAtRoute("DefaultApi", new { id = customer.ID }, customer);
        }

        // DELETE: api/Customers/5
        [ResponseType(typeof(Customer))]
        public async Task<IHttpActionResult> DeleteCustomer(string id)
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

        private bool CustomerExists(string id)
        {
            return db.Customers.Count(e => e.ID == id) > 0;
        }

        private bool IsSubscribed(string customerId, int nicheId)
        {
            return db.Subscriptions.Count(x => x.NicheID == nicheId && x.CustomerID == customerId) > 0;
        }

        private async Task<Subscription> GetSubscription(string email, int nicheId)
        {
            int currentCampaignID = await db.Campaigns.Where(x => x.NicheID == nicheId).Select(x => x.ID).FirstOrDefaultAsync();

            Subscription subscription = new Subscription()
            {
                CustomerID = email,
                NicheID = nicheId,
                CurrentCampaignID = currentCampaignID,
                CurrentEmailDay = 1,
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