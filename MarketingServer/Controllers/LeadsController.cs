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
    public class LeadsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Leads
        public IQueryable<Lead> GetLeads()
        {
            return db.Leads;
        }

        // GET: api/Leads/5
        [ResponseType(typeof(Lead))]
        public async Task<IHttpActionResult> GetLead(string leadPage)
        {
            var lead = await db.Leads.Where(x => x.LeadPage == leadPage).Select(x => new
            {
                leadId = x.ID,
                leadMagnet = x.LeadMagnet,
                mainStyle = x.MainStyle,
                image = x.Image,
                text = x.Text,
                textStyle = x.TextStyle,
                barStyle = x.BarStyle,
                barText = x.BarText,
                buttonStyle = x.ButtonStyle,
                buttonText = x.ButtonText,
                formButtonText = x.FormButtonText
            }).SingleOrDefaultAsync();


            if (lead == null)
            {
                return NotFound();
            }

            return Ok(lead);
        }

        // PUT: api/Leads/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutLead(int id, Lead lead)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != lead.ID)
            {
                return BadRequest();
            }

            db.Entry(lead).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LeadExists(id))
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

        // POST: api/Leads
        [ResponseType(typeof(Lead))]
        public async Task<IHttpActionResult> PostLead(Lead lead)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Leads.Add(lead);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = lead.ID }, lead);
        }

        // DELETE: api/Leads/5
        [ResponseType(typeof(Lead))]
        public async Task<IHttpActionResult> DeleteLead(int id)
        {
            Lead lead = await db.Leads.FindAsync(id);
            if (lead == null)
            {
                return NotFound();
            }

            db.Leads.Remove(lead);
            await db.SaveChangesAsync();

            return Ok(lead);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool LeadExists(int id)
        {
            return db.Leads.Count(e => e.ID == id) > 0;
        }
    }
}