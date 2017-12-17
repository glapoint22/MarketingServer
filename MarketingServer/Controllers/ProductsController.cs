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
                        .Where(z => z.NicheID == x.ID && z.Active)
                        .Select(z => new
                        {
                            id = z.ID,
                            name = z.Name,
                            hopLink = z.HopLink,
                            description = z.Description,
                            image = z.Image,
                            price = z.Price,
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
                            .Contains(z.ID) && z.Active)
                        .Select(z => new {
                            id = z.ID,
                            name = z.Name,
                            hopLink = z.HopLink + "?tid=" + customerId + z.ID,
                            description = z.Description,
                            image = z.Image,
                            price = z.Price,
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

        

        IQueryable<Product> SearchProducts(string searchWords, int category, string language, string productType, string billing, int nicheId)
        {
            IQueryable<Product> query = db.Products.Where(x => x.Active);

            //Search words
            string[] searchWordsArray = searchWords.Split(' ');
            query = query.Where(x => searchWordsArray.All(z => x.Name.Contains(z)));

            //Category
            if (category > 0)
            {
                query = query.Where(x => x.Nich.CategoryID == category);
            }

            //Language
            if(language != string.Empty)
            {
                string[] languages = language.Split(',');

                //English
                if (languages.Contains("English"))
                {
                    query = query.Where(x => x.English);
                }

                //German
                if (languages.Contains("German"))
                {
                    query = query.Where(x => x.German);
                }

                //Spanish
                if (languages.Contains("Spanish"))
                {
                    query = query.Where(x => x.Spanish);
                }

                //French
                if (languages.Contains("French"))
                {
                    query = query.Where(x => x.French);
                }

                //Italian
                if (languages.Contains("Italian"))
                {
                    query = query.Where(x => x.Italian);
                }

                //Portuguese
                if (languages.Contains("Portuguese"))
                {
                    query = query.Where(x => x.Portuguese);
                }
            }

            //Product Types
            if(productType != string.Empty)
            {
                string[] productTypes = productType.Split(',');

                //Digital Download
                if (productTypes.Contains("Digital Download"))
                {
                    query = query.Where(x => x.DigitalDownload);
                }

                //Shippable
                if (productTypes.Contains("Shippable"))
                {
                    query = query.Where(x => x.Shippable);
                }
            }

            //Billing
            if (billing != string.Empty)
            {
                string[] billingTypes = billing.Split(',');

                //Single Payment
                if (billingTypes.Contains("Single Payment"))
                {
                    query = query.Where(x => x.SinglePayment);
                }

                //Subscription
                if (billingTypes.Contains("Subscription"))
                {
                    query = query.Where(x => x.Subscription);
                }

                //Trial
                if (billingTypes.Contains("Trial"))
                {
                    query = query.Where(x => x.Trial);
                }
            }

            //Niche
            if(nicheId > 0)
            {
                query = query.Where(x => x.NicheID == nicheId);
            }

            return query;
        }

        public async Task<IHttpActionResult> GetProductsFromSearch(string query, int category, string language = "", int page = 1, string productType = "", string billing = "", int nicheId = 0)
        {
            int resultsPerPage = 20;
            int currentPage;
            IQueryable<Product> productsQuery = SearchProducts(query, category, language, productType, billing, nicheId);
            var products = await productsQuery.Select(a => a.NicheID).ToListAsync();

            var data = new
            {
                resultsPerPage = resultsPerPage,
                totalProducts = products.Count(),
                page = currentPage = page > 0 && page <= Math.Ceiling((double)products.Count() / resultsPerPage) ? page : 1,
                products = await productsQuery
                    .Select(x => new
                    {
                        id = x.ID,
                        name = x.Name,
                        hopLink = x.HopLink,
                        description = x.Description,
                        image = x.Image,
                        price = x.Price,
                        videos = db.ProductVideos
                            .Where(y => y.ProductID == x.ID)
                            .Select(y => y.Url)
                            .ToList()
                    })
                    .OrderBy(x => x.name)
                    .Skip((currentPage - 1) * resultsPerPage)
                    .Take(resultsPerPage)
                    .ToListAsync(),
                categories = await db.Categories
                    .Where(x => x.Niches
                        .Where(z => products
                            .Contains(z.ID)
                        )
                        .Select(y => y.CategoryID)
                        .ToList()
                        .Contains(x.ID)
                    )
                    .Select(x => new
                    {
                        id = x.ID,
                        name = x.Name,
                        niches = x.Niches
                        .Where(z => products
                            .Contains(z.ID)
                        )
                        .Select(c => new {
                            productCount = productsQuery.Where(a => a.NicheID == c.ID).Count(),
                            id = c.ID,
                            name = c.Name
                        })
                        .ToList()
                    })
                    .ToListAsync()

            };

            return Ok(data);
        }


        IQueryable<Product> Foo(IQueryable<Product> query, int nicheId)
        {
            query = query.Where(x => x.NicheID == nicheId);

            return query;
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
        //public async Task<IHttpActionResult> PostProduct(Product product)
        //{
        //    var categories = await db.Categories
        //        .Select(x => new
        //        {
        //            id = x.ID,
        //            name = x.Name
        //        })
        //        .ToListAsync();

        //    foreach (var category in categories)
        //    {
        //        var niches = await db.Niches
        //            .Where(x => x.CategoryID == category.id)
        //            .Select(x => new
        //            {
        //                id = x.ID,
        //                name = x.Name
        //            })
        //            .ToListAsync();

        //        foreach (var niche in niches)
        //        {
        //            int order = 1;
        //            Random rnd = new Random();
        //            int count = rnd.Next(10, 25);

        //            string[] images = new string[] { "2WeekDiet.gif", "ad2.jpg", "book.png", "box-medium.jpg", "diabetes-lie-3d.png", "EatStopEat.png", "hnm2.jpg", "leanbelly.png", "organifi.png", "Unlock-Your-Hip-Flexors.png", "WakeUpLean.png", "ynm3.jpg" };

        //            for (int i = 0; i < count; i++)
        //            {
        //                Product p = new Product
        //                {
        //                    ID = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
        //                    Name = niche.name + " " + order,
        //                    NicheID = niche.id,
        //                    HopLink = "http://56e2c0n4zhqi1se007udp9fq11.hop.clickbank.net/",
        //                    Order = order,
        //                    Description = "A Foolproof, Science-Based System that's Guaranteed to Melt Away All Your Unwanted Stubborn Body Fat in Just 14 Days.",
        //                    Image = images[rnd.Next(0, 12)],
        //                    Active = true,
        //                    VendorID = 1,
        //                    Price = (decimal)(rnd.NextDouble() * (100.00 - 3.00) + 3.00),
        //                    DigitalDownload = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    Shippable = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    German = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    English = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    Spanish = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    French = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    Italian = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    Portuguese = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    SinglePayment = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    Subscription = Convert.ToBoolean(rnd.Next(0, 2)),
        //                    Trial = Convert.ToBoolean(rnd.Next(0, 2))
        //                };

        //                int day = 1;
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    EmailCampaign e = new EmailCampaign
        //                    {
        //                        ID = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
        //                        ProductID = p.ID,
        //                        Day = j + 1,
        //                        Subject = p.Name + " Day " + day,
        //                        Body = p.Name + " Day " + day
        //                    };
        //                    db.EmailCampaigns.Add(e);
        //                    day++;
        //                }

        //                db.Products.Add(p);
        //                order++;
        //            }
        //        }
        //    }


        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateException)
        //    {
        //        throw;
        //    }

        //    return CreatedAtRoute("DefaultApi", new { id = product.ID }, product);
        //}

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

