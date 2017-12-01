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

namespace MarketingServer
{
    public class ProductsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Products
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> GetProducts()
        {
            var products = await db.Niches
                .OrderByDescending(x => x.Products.Count())
                .Take(2)
                .Select(x => new
                {
                    caption = x.Name,
                    products = db.Products
                        .Where(z => z.NicheID == x.ID)
                        .Select(z => new
                        {
                            id = z.ID,
                            name = z.Name,
                            hopLink = z.HopLink,
                            description = z.Description,
                            image = z.Image,
                            videos = db.ProductVideos
                                .Where(y => y.ProductID == z.ID)
                                .Select(y => y.Url)
                                .ToList()
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/5
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> GetProducts(string customerId)
        {
            var products = await db.Subscriptions
                .Where(x => x.CustomerID == customerId)
                .Select(x => new
                {
                    caption = "Recommendations for you in " + x.Nich.Name,
                    products = db.Products
                        .Where(z => z.NicheID == x.NicheID && !z.CampaignRecords
                            .Where(a => a.SubscriptionID == x.ID)
                            .Select(a => a.ProductID)
                            .ToList()
                            .Contains(z.ID))
                        .Select(z => new {
                            id = z.ID,
                            name = z.Name,
                            hopLink = z.HopLink + "?tid=" + customerId + z.ID,
                            description = z.Description,
                            image = z.Image,
                            videos = db.ProductVideos
                                .Where(y => y.ProductID == z.ID)
                                .Select(y => y.Url)
                                .ToList()
                        })
                        .ToList()
                })
                .ToListAsync();
            


            return Ok(products);
        }

        public async Task<IHttpActionResult> GetProductsFromSearch(string query, int category, int page)
        {
            int resultsPerPage = 20;
            int numProducts;
            int currentPage;

            var data = new
            {
                resultsPerPage = resultsPerPage,
                totalProducts = numProducts = db.Products.Count(x => x.Name.Contains(query) && (category != 0 ? x.Nich.CategoryID == category : 1 == 1)),
                page = currentPage = page > 0 && page <= Math.Ceiling((double)numProducts / resultsPerPage) ? page : 1,
                products = await db.Products
                .Where(x => x.Name.Contains(query) && (category != 0 ? x.Nich.CategoryID == category : 1 == 1))
                .Select(x => new {
                    id = x.ID,
                    name = x.Name,
                    hopLink = x.HopLink,
                    description = x.Description,
                    image = x.Image,
                    videos = db.ProductVideos
                        .Where(y => y.ProductID == x.ID)
                        .Select(y => y.Url)
                        .ToList()
                })
                .OrderBy(x => x.name)
                .Skip((currentPage - 1) * resultsPerPage)
                .Take(resultsPerPage)
                .ToListAsync()
            };

            return Ok(data);
        }

        // PUT: api/Products/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutProduct(string id, Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != product.ID)
            {
                return BadRequest();
            }

            db.Entry(product).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
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

        //POST: api/Products
       //[ResponseType(typeof(Product))]
       // public async Task<IHttpActionResult> PostProduct(Product product)
       // {
       //     var categories = await db.Categories
       //         .Select(x => new
       //         {
       //             id = x.ID,
       //             name = x.Name
       //         })
       //         .ToListAsync();

       //     foreach (var category in categories)
       //     {
       //         var niches = await db.Niches
       //             .Where(x => x.CategoryID == category.id)
       //             .Select(x => new
       //             {
       //                 id = x.ID,
       //                 name = x.Name
       //             })
       //             .ToListAsync();

       //         foreach (var niche in niches)
       //         {
       //             int order = 1;
       //             Random rnd = new Random();
       //             int count = rnd.Next(10, 25);

       //             for (int i = 0; i < count; i++)
       //             {
       //                 Product p = new Product
       //                 {
       //                     ID = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
       //                     Name = niche.name + " " + order,
       //                     NicheID = niche.id,
       //                     HopLink = "http://56e2c0n4zhqi1se007udp9fq11.hop.clickbank.net/",
       //                     Order = order,
       //                     Description = "A Foolproof, Science-Based System that's Guaranteed to Melt Away All Your Unwanted Stubborn Body Fat in Just 14 Days.",
       //                     Image = "2WeekDiet.gif"
       //                 };

       //                 int day = 1;
       //                 for (int j = 0; j < 4; j++)
       //                 {
       //                     EmailCampaign e = new EmailCampaign
       //                     {
       //                         ID = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
       //                         ProductID = p.ID,
       //                         Day = j + 1,
       //                         Subject = p.Name + " Day " + day,
       //                         Body = p.Name + " Day " + day
       //                     };
       //                     db.EmailCampaigns.Add(e);
       //                     day++;
       //                 }

       //                 db.Products.Add(p);
       //                 order++;
       //             }
       //         }
       //     }


       //     try
       //     {
       //         await db.SaveChangesAsync();
       //     }
       //     catch (DbUpdateException)
       //     {
       //         throw;
       //     }

       //     return CreatedAtRoute("DefaultApi", new { id = product.ID }, product);
       // }

        // DELETE: api/Products/5
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> DeleteProduct(string id)
        {
            Product product = await db.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            db.Products.Remove(product);
            await db.SaveChangesAsync();

            return Ok(product);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ProductExists(string id)
        {
            return db.Products.Count(e => e.ID == id) > 0;
        }
    }
}