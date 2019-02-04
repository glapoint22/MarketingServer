using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Data.Entity.SqlServer;
using System.Net.Http;
using System.Net;

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

        [Route("api/Customers/Session")]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> GetSession()
        {
            HttpResponseMessage response = new HttpResponseMessage();

            string sessionId;
            Customer customer = null;

            sessionId = Session.GetSessionID(Request.Headers);

            if (sessionId != null) customer = await db.Customers.Where(x => x.SessionID == sessionId).FirstOrDefaultAsync();

            if(customer == null)
            {
                return response;
            }

            sessionId = Guid.NewGuid().ToString("N");

            customer.SessionID = Hashing.GetHash(sessionId);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }

            Session.SetSessionID(sessionId, Request, ref response);

            return response;
        }


    }
}