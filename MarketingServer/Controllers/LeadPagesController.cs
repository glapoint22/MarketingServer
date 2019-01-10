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
using System.Web;

namespace MarketingServer.Controllers
{
    public class LeadPagesController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/LeadPages
        public async Task<IHttpActionResult> GetLeadPages()
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
                        leadPages = z.LeadPages
                                .Where(a => a.NicheID == z.ID)
                                .Select(a => new {
                                    id = a.ID,
                                    title = a.Title,
                                    body = a.Body,
                                    pageTitle = a.PageTitle,
                                    leadMagnet = a.LeadMagnet
                                })
                                .ToList()
                    }).ToList()
                }
            )
            .ToListAsync();

            return Ok(categories);
        }

        // GET: api/LeadPages/5
        [ResponseType(typeof(LeadPage))]
        public async Task<IHttpActionResult> GetLeadPage(string pageTitle)
        {
            LeadPage leadPage = await db.LeadPages.FindAsync(pageTitle);
            if (leadPage == null)
            {
                return NotFound();
            }

            return Ok(leadPage);
        }

        // PUT: api/LeadPages/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutLeadPage(string id, LeadPage leadPage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != leadPage.ID)
            {
                return BadRequest();
            }

            db.Entry(leadPage).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LeadPageExists(id))
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

        // POST: api/LeadPages
        [HttpPost]
        public HttpResponseMessage PostImage()
        {
            HttpPostedFile postedFile = HttpContext.Current.Request.Files["file"];
            string filePath = HttpContext.Current.Server.MapPath("~/Downloads/" + postedFile.FileName);

            //int imageIndex = filePath.LastIndexOf("\\") + 1;
            //string imageName = filePath.Substring(imageIndex);
            //int imageExt = imageName.IndexOf(".");

            //string newImageName = Guid.NewGuid().ToString("N") + imageName.Substring(imageExt);

            //filePath = filePath.Substring(0, imageIndex) + newImageName;


            postedFile.SaveAs(filePath);


            return Request.CreateResponse(HttpStatusCode.OK, postedFile.FileName);
        }
        //[ResponseType(typeof(LeadPage))]
        //public async Task<IHttpActionResult> PostLeadPage(LeadPage leadPage)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    db.LeadPages.Add(leadPage);

        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateException)
        //    {
        //        if (LeadPageExists(leadPage.ID))
        //        {
        //            return Conflict();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return CreatedAtRoute("DefaultApi", new { id = leadPage.ID }, leadPage);
        //}

        // DELETE: api/LeadPages/5
        [ResponseType(typeof(LeadPage))]
        public async Task<IHttpActionResult> DeleteLeadPage(string id)
        {
            LeadPage leadPage = await db.LeadPages.FindAsync(id);
            if (leadPage == null)
            {
                return NotFound();
            }

            db.LeadPages.Remove(leadPage);
            await db.SaveChangesAsync();

            return Ok(leadPage);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool LeadPageExists(string id)
        {
            return db.LeadPages.Count(e => e.ID == id) > 0;
        }
    }
}