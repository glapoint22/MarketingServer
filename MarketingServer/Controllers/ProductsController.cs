using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace MarketingServer
{
    public class ProductsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Products
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> GetProducts()
        {
            List<object> products = new List<object>();
            products.Add(await GetFeaturedProducts());

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
                        .Select(z => new
                        {
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


            products.Insert(0, await GetFeaturedProducts());

            return Ok(products);
        }

        private async Task<dynamic> GetFeaturedProducts()
        {
            return new
            {
                caption = "Check out our featured products",
                products = await db.Products
                        .Where(x => x.Active && x.Featured)
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
                        .ToListAsync()
            };
        }


        IQueryable<Product> BuildQuery(string searchWords, int category, int nicheId, string queryFilters, List<Filter> filterList, List<PriceRange> priceRanges, string filterExclude = "")
        {
            IQueryable<Product> query = db.Products.Where(x => x.Active);
            char separator = '^';

            //Search words
            if(searchWords != string.Empty)
            {
                string[] searchWordsArray = searchWords.Split(' ');
                query = query.Where(x => searchWordsArray.All(z => x.Name.Contains(z)));
            }
            

            //Category
            if (category > 0)
            {
                query = query.Where(x => x.Nich.CategoryID == category);
            }

            
            //Niche
            if (nicheId > 0)
            {
                query = query.Where(x => x.NicheID == nicheId);
            }


            //Filters
            if (queryFilters != string.Empty)
            {
                Match result;

                //Price Filter
                if(filterExclude != "Price")
                {
                    result = Regex.Match(queryFilters, GetRegExPattern("Price"));
                    if (result.Length > 0)
                    {
                        string[] priceRangeArray = result.Groups[2].Value.Split(separator);
                        List<PriceRange> priceRangeList = priceRanges
                            .Where(x => priceRangeArray.Contains(x.Label))
                            .Select(x => new PriceRange
                            {
                                Min = x.Min,
                                Max = x.Max
                            }).ToList();

                        foreach(string price in priceRangeArray)
                        {
                            result = Regex.Match(price, @"\[(\d+\.?(?:\d+)?)-(\d+\.?(?:\d+)?)\]");
                            if (result.Length > 0)
                            {
                                PriceRange range = new PriceRange
                                {
                                    Min = double.Parse(result.Groups[1].Value),
                                    Max = double.Parse(result.Groups[2].Value),
                                };
                                priceRangeList.Add(range);
                            }
                        }

                        Expression<Func<Product, bool>> predicate = ExpressionBuilder.False<Product>();

                        foreach (PriceRange priceRange in priceRangeList)
                        {
                            PriceRange temp = priceRange;
                            predicate = predicate.Or(x => x.Price >= temp.Min && x.Price <= temp.Max);
                        }

                        query = query.Where(predicate);
                    }
                }
                


                //Custom Filters
                foreach (Filter currentFilter in filterList)
                {
                    if (filterExclude != currentFilter.Name)
                    {
                        result = Regex.Match(queryFilters, GetRegExPattern(currentFilter.Name));

                        if (result.Length > 0)
                        {
                            //Get the options chosen from this filter
                            string[] optionsArray = result.Groups[2].Value.Split(separator);

                            //Get a list of ids from the options array
                            List<int> optionIdList = currentFilter.FilterLabels.Where(x => optionsArray.Contains(x.Name)).Select(x => x.ID).ToList();

                            //Set the query
                            query = query
                                .Where(x => x.ProductFilters
                                    .Where(z => optionIdList.Contains(z.FilterLabelID))
                                    .Select(z => z.ProductID)
                                    .ToList()
                                    .Contains(x.ID)
                                );
                        }
                    }
                }
            }


            return query;
        }

        private string GetRegExPattern(string filterName)
        {
            return "(" + filterName + "\\|)([a-zA-Z0-9`~!@#$%^&*()\\-_+={[}\\]\\:;\"\'<,>.?/\\s]+)";
        }

        private async Task<List<FilterData>> GetFilters(string searchWords, int category, int nicheId, string queryFilters, List<Filter> filterList, List<PriceRange> priceRanges)
        {
            List<FilterData> filters = new List<FilterData>();
            List<Label> labels;
            FilterData filter;
            string exclude = "";


            //Create labels for the price filter
            labels = new List<Label>();
            if (Regex.Match(queryFilters, GetRegExPattern("Price")).Length > 0) exclude = "Price";

            IQueryable<Product> query = BuildQuery(searchWords, category, nicheId, queryFilters, filterList, priceRanges, exclude);

            foreach (PriceRange priceRange in priceRanges)
            {
                int count = await query.CountAsync(x => x.Price >= priceRange.Min && x.Price <= priceRange.Max);
                if (count > 0)
                {
                    Label label = new Label
                    {
                        name = priceRange.Label,
                        productCount = count
                    };
                    labels.Add(label);
                }
            }

            //Create the price filter
            filter = new FilterData
            {
                caption = "Price",
                labels = labels
            };

            filters.Add(filter);



            foreach (Filter currentFilter in filterList)
            {
                //Create the labels for the current filter
                labels = new List<Label>();
                exclude = "";
                if (Regex.Match(queryFilters, GetRegExPattern(currentFilter.Name)).Length > 0) exclude = currentFilter.Name;

                query = BuildQuery(searchWords, category, nicheId, queryFilters, filterList, priceRanges, exclude);

                foreach (FilterLabel filterLabel in currentFilter.FilterLabels)
                {
                    int count = await query.CountAsync(x => x.ProductFilters
                        .Where(z => z.FilterLabelID == filterLabel.ID)
                        .Select(z => z.ProductID)
                        .ToList()
                        .Contains(x.ID)
                    );
                    //If the count is greater than zero, create the label
                    if (count > 0)
                    {
                        Label label = new Label
                        {
                            name = filterLabel.Name,
                            productCount = count,
                        };
                        labels.Add(label);
                    }
                }

                //If there are any labels, create the filter
                if (labels.Count > 0)
                {
                    filter = new FilterData
                    {
                        caption = currentFilter.Name,
                        labels = labels
                    };

                    filters.Add(filter);
                }
            }

            return filters;
        }

        public async Task<IHttpActionResult> GetProductsFromSearch(int category, string sort, int limit, string query = "", int nicheId = 0, int page = 1, string filter = "")
        {
            int currentPage;

            List<PriceRange> priceRanges = await db.PriceRanges.ToListAsync();
            List<Filter> filterList = await db.Filters.ToListAsync();
            IQueryable<Product> productsQuery = BuildQuery(query, category, nicheId, filter, filterList, priceRanges);
            List<int> products = await productsQuery.Select(a => a.NicheID).ToListAsync();

            var data = new
            {
                totalProducts = products.Count(),
                page = currentPage = page > 0 && page <= Math.Ceiling((double)products.Count() / limit) ? page : 1,
                products = await productsQuery
                    .OrderBy(sort, query)
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
                    .Skip((currentPage - 1) * limit)
                    .Take(limit)
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
                        .Select(c => new
                        {
                            productCount = productsQuery.Count(a => a.NicheID == c.ID),
                            id = c.ID,
                            name = c.Name
                        })
                        .ToList()
                    })
                    .ToListAsync(),
                filters = await GetFilters(query, category, nicheId, filter, filterList, priceRanges)

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
        //public async Task<IHttpActionResult> PostProduct(Product product)
        //{
        //    string[] banners = new string[] {
        //        "Fall.jpg",
        //        "Costumes.png",
        //        "2WeekDiet.png",
        //        "Halloween.png",
        //        "Delight.jpg"
        //    };

        //    string[] videos = new string[] {
        //        "//player.vimeo.com/video/203810510?title=0&byline=0&portrait=0&color=ffffff",
        //        "//player.vimeo.com/video/195471382?title=0&byline=0&portrait=0&color=ffffff",
        //        "//player.vimeo.com/video/196271312?title=0&byline=0&portrait=0&color=ffffff",
        //        "//player.vimeo.com/video/195492285?title=0&byline=0&portrait=0&color=ffffff",
        //        "//player.vimeo.com/video/197072042?title=0&byline=0&portrait=0&color=ffffff",
        //        "//player.vimeo.com/video/195506334?title=0&byline=0&portrait=0&color=ffffff",
        //        "//player.vimeo.com/video/195494203?title=0&byline=0&portrait=0&color=ffffff"
        //    };


        //    int totalProducts = 0;

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
        //                };

        //                if (rnd.Next(0, 2) == 1)
        //                {
        //                    for(int z = 0; z < videos.Length; z++)
        //                    {
        //                        if (rnd.Next(0, 2) == 1)
        //                        {
        //                            ProductVideo productVideo = new ProductVideo
        //                            {
        //                                ProductID = p.ID,
        //                                Url = videos[z]
        //                            };
        //                            db.ProductVideos.Add(productVideo);
        //                        }
        //                    }

        //                }

        //                    for (int k = 17; k < 28; k++)
        //                {
        //                    if(rnd.Next(0, 2) == 1)
        //                    {
        //                        ProductFilterOption productFilterOption = new ProductFilterOption
        //                        {
        //                            ProductID = p.ID,
        //                            FilterOptionID = k
        //                        };
        //                        db.ProductFilterOptions.Add(productFilterOption);
        //                    }
        //                }

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
        //                totalProducts++;
        //                if (totalProducts < 6)
        //                {

        //                    ProductBanner productBanner = new ProductBanner
        //                    {
        //                        ProductID = p.ID,
        //                        Name = banners[totalProducts - 1],
        //                        Selected = true
        //                    };

        //                    db.ProductBanners.Add(productBanner);

        //                }
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


    public struct Label
    {
        public string name;
        public int productCount;
        public bool @checked;
    }

    public struct FilterData
    {
        public string caption;
        public List<Label> labels;
    }

    //class SwapVisitor : System.Linq.Expressions.ExpressionVisitor
    //{
    //    private readonly Expression from, to;
    //    public SwapVisitor(Expression from, Expression to)
    //    {
    //        this.from = from;
    //        this.to = to;
    //    }
    //    public override Expression Visit(Expression node)
    //    {
    //        return node == from ? to : base.Visit(node);
    //    }
    //}
}

