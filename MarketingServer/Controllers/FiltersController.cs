using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace MarketingServer.Controllers
{
    public class FiltersController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Filters
        public async Task<IHttpActionResult> GetFilters()
        {
            var filters = await db.Filters
                .AsNoTracking()
                .Select(x => new
                {
                    id = x.ID,
                    name = x.Name,
                    options = x.FilterLabels
                    .Where(y => y.FilterID == x.ID)
                    .Select(y => new
                    {
                        id = y.ID,
                        name = y.Name
                    })
                    .ToList()
                })
            .ToListAsync();

            return Ok(filters);
        }

        // PUT: api/Filters/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutFilter(Filter[] filters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (Filter filter in filters)
            {
                db.Entry(filter).State = EntityState.Modified;
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        // POST: api/Filters
        [ResponseType(typeof(Filter))]
        public async Task<IHttpActionResult> PostFilter(Filter[] filters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (Filter filter in filters)
            {
                db.Filters.Add(filter);
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Filters/5
        [ResponseType(typeof(Filter))]
        public async Task<IHttpActionResult> DeleteFilter(string itemIds)
        {
            string[] ids = itemIds.Split(',');

            foreach (string id in ids)
            {
                Filter filter = await db.Filters.FindAsync(int.Parse(id));
                if (filter == null)
                {
                    return NotFound();
                }

                db.Filters.Remove(filter);
            }

            await db.SaveChangesAsync();
            return Ok();
        }
    }
}