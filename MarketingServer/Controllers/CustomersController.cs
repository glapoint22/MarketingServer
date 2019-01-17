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
using System.Data.Entity.SqlServer;

namespace MarketingServer.Controllers
{
    public class CustomersController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Customers
        [ResponseType(typeof(Customer))]
        public async Task<IHttpActionResult> GetCustomers()
        {
            var customers = await db.Customers
                    .AsNoTracking()
                    .Where(c => SqlFunctions.DateDiff("day", c.EmailSentDate, DateTime.Today) >= c.EmailSendFrequency && c.Subscriptions
                        .Where(x => x.Subscribed && !x.Suspended).Count() != 0)
                        .Select(x => new
                        {
                            ID = x.ID,
                            Name = x.Name,
                            Email = x.Email,
                            EmailSendFrequency = x.EmailSendFrequency,
                            EmailSentDate = x.EmailSentDate,
                            Subscriptions = x.Subscriptions.Select(z => new
                            {
                                ID = z.ID,
                                CustomerID = z.CustomerID,
                                NicheID = z.NicheID,
                                Subscribed = z.Subscribed,
                                Suspended = z.Suspended,
                                DateSubscribed = z.DateSubscribed,
                                DateUnsubscribed = z.DateUnsubscribed
                            }).ToList()
                        })
                    .ToListAsync();

            return Ok(customers);
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
        public async Task<IHttpActionResult> PostCustomer(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Customers.Add(customer);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (CustomerExists(customer.ID))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
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
    }
}