using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace MarketingServer.Controllers
{
    public class FilterLabelsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // PUT: api/FilterLabels/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutFilterLabel(FilterLabel[] filterLabels)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach(FilterLabel filterLabel in filterLabels)
            {
                db.Entry(filterLabel).State = EntityState.Modified;
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        // POST: api/FilterLabels
        [ResponseType(typeof(FilterLabel))]
        public async Task<IHttpActionResult> PostFilterLabel(FilterLabel[] filterLabels)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach(FilterLabel filterLabel in filterLabels)
            {
                db.FilterLabels.Add(filterLabel);
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/FilterLabels/5
        [ResponseType(typeof(FilterLabel))]
        public async Task<IHttpActionResult> DeleteFilterLabel(string itemIds)
        {
            string[] ids = itemIds.Split(',');

            foreach (string id in ids)
            {
                FilterLabel filterLabel = await db.FilterLabels.FindAsync(int.Parse(id));
                if (filterLabel == null)
                {
                    return NotFound();
                }

                db.FilterLabels.Remove(filterLabel);
            }

            await db.SaveChangesAsync();
            return Ok();
        }
    }
}