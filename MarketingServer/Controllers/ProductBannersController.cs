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
            string sessionId;
            string customerId = null;

            sessionId = Session.GetSessionID(Request.Headers);

            if (sessionId != null) customerId = await db.Customers.AsNoTracking().Where(x => x.SessionID == sessionId).Select(x => x.ID).FirstOrDefaultAsync();


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
                        hopLink = x.Product.HopLink + (customerId != null ? (x.Product.HopLink.IndexOf("?") == -1 ? "?" : "&") + "tid=" + customerId + x.Product.ID : "")
                    }
                })
                .ToListAsync();

            return Ok(productBanners);
        }
    }
}