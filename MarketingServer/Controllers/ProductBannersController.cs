using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace MarketingServer
{
    public class ProductBannersController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/ProductBanners
        [AllowAnonymous]
        public async Task<IHttpActionResult> GetProductBanners()
        {
            var productBanners = await db.ProductBanners
                .AsNoTracking()
                .Where(x => x.Selected)
                .Select(x => new
                {
                    name = x.Name,
                    product = new
                    {
                        id = x.Product.ID,
                        name = x.Product.Name,
                        hopLink = x.Product.HopLink
                    }
                })
                .ToListAsync();

            return Ok(productBanners);
        }

        // GET: api/ProductBanners/5
        //[ResponseType(typeof(ProductBanner))]
        //public async Task<IHttpActionResult> GetProductBanner(string id)
        //{
        //    ProductBanner productBanner = await db.ProductBanners.FindAsync(id);
        //    if (productBanner == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(productBanner);
        //}

        // PUT: api/ProductBanners/5
        //[ResponseType(typeof(void))]
        //public async Task<IHttpActionResult> PutProductBanner(string id, ProductBanner productBanner)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != productBanner.ProductID)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(productBanner).State = EntityState.Modified;

        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!ProductBannerExists(id))
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

        // POST: api/ProductBanners
        //[ResponseType(typeof(ProductBanner))]
        //public async Task<IHttpActionResult> PostProductBanner(ProductBanner productBanner)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    db.ProductBanners.Add(productBanner);

        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateException)
        //    {
        //        if (ProductBannerExists(productBanner.ProductID))
        //        {
        //            return Conflict();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return CreatedAtRoute("DefaultApi", new { id = productBanner.ProductID }, productBanner);
        //}

        // DELETE: api/ProductBanners/5
        //[ResponseType(typeof(ProductBanner))]
        //public async Task<IHttpActionResult> DeleteProductBanner(string id)
        //{
        //    ProductBanner productBanner = await db.ProductBanners.FindAsync(id);
        //    if (productBanner == null)
        //    {
        //        return NotFound();
        //    }

        //    db.ProductBanners.Remove(productBanner);
        //    await db.SaveChangesAsync();

        //    return Ok(productBanner);
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        //private bool ProductBannerExists(string id)
        //{
        //    return db.ProductBanners.Count(e => e.ProductID == id) > 0;
        //}
    }
}