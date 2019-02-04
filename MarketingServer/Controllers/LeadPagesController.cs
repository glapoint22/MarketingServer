using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
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
                        leadMagnetEmails = z.LeadMagnetEmails
                                .Where(a => a.NicheID == z.ID)
                                .Select(a => new
                                {
                                    id = a.ID,
                                    subject = a.Subject,
                                    body = a.Body,
                                    nicheId = z.ID
                                })
                                .ToList(),
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
        //[ResponseType(typeof(LeadPage))]
        [AllowAnonymous]
        public async Task<IHttpActionResult> GetLeadPage(string pageTitle)
        {
            var leadPage = await db.LeadPages.AsNoTracking().Where(x => x.PageTitle == pageTitle).Select(x => new
            {
                title = x.Title,
                body = x.Body,
                pageTitle = x.PageTitle,
                leadMagnet = x.LeadMagnet,
                nicheId = x.NicheID
            })
            .SingleOrDefaultAsync();

            if (leadPage == null)
            {
                return NotFound();
            }

            return Ok(leadPage);
        }

        // POST: api/LeadPages
        [HttpPost]
        public HttpResponseMessage PostLead()
        {
            HttpPostedFile postedFile = HttpContext.Current.Request.Files["file"];
            string filePath = HttpContext.Current.Server.MapPath("~/Downloads/" + postedFile.FileName);
            postedFile.SaveAs(filePath);

            return Request.CreateResponse(HttpStatusCode.OK, postedFile.FileName);
        }
        
    }
}