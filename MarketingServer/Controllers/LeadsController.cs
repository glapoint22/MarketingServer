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
        public async Task<IHttpActionResult> GetLeads()
        {
            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    id = x.ID,
                    name = x.Name,
                    niches = x.Niches
                    .Select(z => new
                    {
                        id = z.ID,
                        name = z.Name,
                        leadMagnet = z.LeadMagnet,
                        leadPage = z.LeadPage,
                        leadMagnetCaption = z.LeadMagnetCaption,
                        leadPageBody = z.LeadPageBody
                    }).ToList()
                }
            )
            .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Leads/5
        //[ResponseType(typeof(Lead))]
        public async Task<IHttpActionResult> GetLead(string leadPage)
        {
            var lead = await db.Niches.Where(x => x.LeadPage == leadPage).Select(x => new
            {
                leadMagnetCaption = x.LeadMagnetCaption,
                leadPageBody = x.LeadPageBody,
                nicheId = x.ID
            }).SingleOrDefaultAsync();


            if (lead == null)
            {
                return NotFound();
            }

            return Ok(lead);
        }

        // PUT: api/Leads/5
        //[ResponseType(typeof(void))]
        //public async Task<IHttpActionResult> PutLead(int id, Lead lead)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != lead.ID)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(lead).State = EntityState.Modified;

        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!LeadExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        //// POST: api/Leads
        //[ResponseType(typeof(Lead))]
        //public async Task<IHttpActionResult> PostLead(Lead lead)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    db.Leads.Add(lead);
        //    await db.SaveChangesAsync();

        //    return CreatedAtRoute("DefaultApi", new { id = lead.ID }, lead);
        //}

        //// DELETE: api/Leads/5
        //[ResponseType(typeof(Lead))]
        //public async Task<IHttpActionResult> DeleteLead(int id)
        //{
        //    Lead lead = await db.Leads.FindAsync(id);
        //    if (lead == null)
        //    {
        //        return NotFound();
        //    }

        //    db.Leads.Remove(lead);
        //    await db.SaveChangesAsync();

        //    return Ok(lead);
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        //private bool LeadExists(int id)
        //{
        //    return db.Leads.Count(e => e.ID == id) > 0;
        //}
    }
}