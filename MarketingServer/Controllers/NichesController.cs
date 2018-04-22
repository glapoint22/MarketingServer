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
    public class NichesController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Niches
        public IQueryable<Nich> GetNiches()
        {
            return db.Niches;
        }

        // GET: api/Niches/5
        [ResponseType(typeof(Nich))]
        public async Task<IHttpActionResult> GetNich(int id)
        {
            Nich nich = await db.Niches.FindAsync(id);
            if (nich == null)
            {
                return NotFound();
            }

            return Ok(nich);
        }

        // PUT: api/Niches/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutNich(Nich[] niches)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (Nich niche in niches)
            {
                db.Entry(niche).State = EntityState.Modified;
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        // POST: api/Niches
        [ResponseType(typeof(Nich))]
        public async Task<IHttpActionResult> PostNich(Nich[] niches)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach(Nich niche in niches)
            {
                db.Niches.Add(niche);
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Niches/5
        [ResponseType(typeof(Nich))]
        public async Task<IHttpActionResult> DeleteNich(int[] ids)
        {
            foreach(int id in ids)
            {
                Nich nich = await db.Niches.FindAsync(id);
                if (nich == null)
                {
                    return NotFound();
                }

                db.Niches.Remove(nich);
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool NichExists(int id)
        {
            return db.Niches.Count(e => e.ID == id) > 0;
        }
    }
}