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
    }
}