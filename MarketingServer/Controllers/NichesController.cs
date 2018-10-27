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
                if (niche.Name == null)
                {
                    List<LeadMagnetEmail> dbLeadMagnetEmails = await db.LeadMagnetEmails.AsNoTracking().Where(x => x.NicheID == niche.ID).ToListAsync();

                    // Check to see if any emails have been deleted
                    foreach (LeadMagnetEmail dbLeadMagnetEmail in dbLeadMagnetEmails)
                    {
                        if (!niche.LeadMagnetEmails.Select(x => x.ID).ToList().Contains(dbLeadMagnetEmail.ID))
                        {
                            db.LeadMagnetEmails.Attach(dbLeadMagnetEmail);
                            db.LeadMagnetEmails.Remove(dbLeadMagnetEmail);

                        }
                    }

                    // Check to see if any lead magnet emails need to be added or have been modified
                    foreach (LeadMagnetEmail leadMagnetEmail in niche.LeadMagnetEmails)
                    {
                        if (!(dbLeadMagnetEmails.Count(x => x.ID == leadMagnetEmail.ID) > 0))
                        {
                            db.Entry(leadMagnetEmail).State = EntityState.Added;
                        }
                        else
                        {
                            LeadMagnetEmail dbLeadMagnetEmail = dbLeadMagnetEmails.FirstOrDefault(x => x.ID == leadMagnetEmail.ID);
                            if (dbLeadMagnetEmail.Subject != leadMagnetEmail.Subject || dbLeadMagnetEmail.Body != leadMagnetEmail.Body)
                            {
                                db.Entry(leadMagnetEmail).State = EntityState.Modified;
                            }
                        }
                    }
                }
                else
                {
                    // Check to see if this niche has been modified
                    Nich dbNiche = db.Niches.AsNoTracking().FirstOrDefault(x => x.ID == niche.ID);
                    if (dbNiche.Name != niche.Name || dbNiche.Icon != niche.Icon)
                    {
                        db.Entry(niche).State = EntityState.Modified;
                    }
                }





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

            foreach (Nich niche in niches)
            {
                db.Niches.Add(niche);
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Niches/5
        [ResponseType(typeof(Nich))]
        public async Task<IHttpActionResult> DeleteNich(string itemIds)
        {
            string[] ids = itemIds.Split(',');

            foreach (string id in ids)
            {
                Nich niche = await db.Niches.FindAsync(int.Parse(id));
                if (niche == null)
                {
                    return NotFound();
                }

                // List of images to delete from the images directory
                List<string> imagesToDelete = new List<string>();

                // Add the niche icon to the list
                if (niche.Icon != null) imagesToDelete.Add(niche.Icon);

                // Add product images to the list
                niche.Products.ToList().ForEach(x =>
                {
                    if (x.Image != null) imagesToDelete.Add(x.Image);
                    x.ProductBanners.ToList().ForEach(y => imagesToDelete.Add(y.Name));
                });

                // Delete the images
                imagesToDelete.ForEach(x => ImageController.DeleteImageFile(x));

                // Remove this niche from the database
                db.Niches.Remove(niche);
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