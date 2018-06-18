using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
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
        public async Task<IHttpActionResult> GetProducts(string customerId, string productIds)
        {
            List<ProductGroup> productGroups = new List<ProductGroup>();
            List<ProductGroup> recommendedProducts = await GetRecommendedProducts(customerId);

            // Featured products
            ProductGroup featuredProducts = await GetFeaturedProducts(customerId);
            if (featuredProducts.products.Count > 0) productGroups.Add(featuredProducts);



            // Recommended Products
            if (customerId != null)
            {
                for (int i = 0; i < recommendedProducts.Count; i++)
                {
                    if (recommendedProducts[i].products.Count > 0) productGroups.Add(recommendedProducts[i]);
                }
            }

            // Browsed products
            if (productIds != null)
            {
                ProductGroup browsedProducts = await GetBrowsedProducts(productIds, customerId, recommendedProducts.SelectMany(x => x.products.Select(y => y.id)).ToList());
                if (browsedProducts.products.Count > 0) productGroups.Add(browsedProducts);
            }

            return Ok(productGroups);
        }

        private async Task<ProductGroup> GetFeaturedProducts(string customerId)
        {
            return new ProductGroup
            {
                caption = "Check out our featured products",
                products = await db.Products
                        .Where(x => x.Featured)
                        .OrderBy(x => x.Name)
                        .Select(z => new ProductData
                        {
                            id = z.ID,
                            name = z.Name,
                            hopLink = z.HopLink + (customerId != null ? "?tid=" + customerId + z.ID : ""),
                            description = z.Description,
                            image = z.Image,
                            price = z.Price,
                            videos = z.ProductVideos
                                .Where(y => y.ProductID == z.ID)
                                .Select(y => y.Url)
                                .ToList()
                        })
                        .ToListAsync()
            };
        }

        private async Task<List<ProductGroup>> GetRecommendedProducts(string customerId)
        {
            return await db.Subscriptions
                .Where(x => x.CustomerID == customerId && x.Subscribed && !x.Suspended)
                .Select(x => new ProductGroup
                {
                    caption = "Recommendations for you in " + x.Nich.Name,
                    products = db.Products
                        .Where(z => z.NicheID == x.NicheID && !z.CampaignRecords
                            .Where(a => a.SubscriptionID == x.ID && a.ProductPurchased)
                            .Select(a => a.ProductID)
                            .ToList()
                            .Contains(z.ID))
                            .OrderBy(z => z.Name)
                        .Select(z => new ProductData
                        {
                            id = z.ID,
                            name = z.Name,
                            hopLink = z.HopLink + "?tid=" + customerId + z.ID,
                            description = z.Description,
                            image = z.Image,
                            price = z.Price,
                            videos = z.ProductVideos
                                .Where(y => y.ProductID == z.ID)
                                .Select(y => y.Url)
                                .ToList()
                        })
                        .ToList()
                })
                .ToListAsync();
        }

        private async Task<ProductGroup> GetBrowsedProducts(string productIds, string customerId, List<string> usedIds)
        {
            int maxCount = 20;

            // Put the product ids into a list
            List<string> ids = productIds.Split('~').ToList();


            // Expression for not suspended
            Expression<Func<Product, bool>> notSuspended = x => !db.Products
                .Where(z => db.Subscriptions
                    .Where(a => a.CustomerID == customerId && a.Suspended)
                    .Select(a => a.NicheID)
                    .ToList()
                    .Contains(z.NicheID))
                .Select(z => z.ID)
                .ToList()
                .Contains(x.ID);

            // Expression for not purchased
            Expression<Func<Product, bool>> notPurchased = x => !x.CampaignRecords
                .Where(a => db.Subscriptions
                    .Where(y => y.CustomerID == customerId)
                    .Select(y => y.ID)
                    .ToList()
                    .Contains(a.SubscriptionID) && a.ProductPurchased)
                .Select(a => a.ProductID)
                .ToList()
                .Contains(x.ID);

            // Get the niche ids based on the product ids
            var tempNicheIds = await db.Products
                .Where(x => ids.Contains(x.ID))
                .Where(x => !usedIds.Contains(x.ID))
                .Where(notSuspended)
                .Select(x => new
                {
                    productId = x.ID,
                    nicheId = x.NicheID
                })
                .ToListAsync();

            // Reorder the nicheids and calculate how many products to display per niche
            List<int> nicheIds = tempNicheIds.OrderByDescending(x => ids.IndexOf(x.productId)).Select(x => x.nicheId).Take(4).Distinct().ToList();
            int productCount = nicheIds.Count > 0 ? maxCount / nicheIds.Count : 0;

            // Get the products based on the niche ids
            var tempProducts = await db.Products
                .Where(x => nicheIds.Contains(x.NicheID))
                .Where(notPurchased)
                .Where(x => !usedIds.Contains(x.ID))
                .GroupBy(x => x.NicheID)
                .Select(x => x.Take(productCount))
                .SelectMany(e => e)
                .Union(db.Products
                    .Where(x => ids.Contains(x.ID) && nicheIds.Contains(x.NicheID))
                    .Where(x => !usedIds.Contains(x.ID))
                    .Where(notPurchased)
                    .Where(notSuspended)
                )
                .OrderByDescending(x => ids.Contains(x.ID))
                .Take(maxCount)
                .Select(x => new
                {
                    nicheId = x.NicheID,
                    id = x.ID,
                    name = x.Name,
                    hopLink = x.HopLink + (customerId != null ? "?tid=" + customerId + x.ID : ""),
                    description = x.Description,
                    image = x.Image,
                    price = x.Price,
                    videos = x.ProductVideos
                        .Where(y => y.ProductID == x.ID)
                        .Select(y => y.Url)
                        .ToList()
                })
                .ToListAsync();

            // Order the products
            List<ProductData> products = tempProducts
                .OrderBy(x => nicheIds.IndexOf(x.nicheId))
                .ThenBy(x => x.name)
                .Select(x => new ProductData
                {
                    id = x.id,
                    name = x.name,
                    hopLink = x.hopLink,
                    description = x.description,
                    image = x.image,
                    price = x.price,
                    videos = x.videos
                }).ToList();

            return new ProductGroup
            {
                caption = "Other products you may like based on your browsing",
                products = products
            };
        }


        IQueryable<Product> BuildQuery(string searchWords, int category, int nicheId, string queryFilters, List<Filter> filterList, List<PriceRange> priceRanges, string filterExclude = "")
        {
            IQueryable<Product> query = db.Products;
            char separator = '^';

            //Search words
            if (searchWords != string.Empty)
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
                if (filterExclude != "Price")
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

                        foreach (string price in priceRangeArray)
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
        public async Task<IHttpActionResult> PutProduct(Product[] products)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (Product product in products)
            {
                // Get a list of product banners, product videos, and product filters for this product
                List<ProductBanner> dbProductBanners = await db.ProductBanners.Where(x => x.ProductID == product.ID).ToListAsync();
                List<ProductVideo> dbProductVideos = await db.ProductVideos.Where(x => x.ProductID == product.ID).ToListAsync();
                List<ProductFilter> dbProductFilters = await db.ProductFilters.Where(x => x.ProductID == product.ID).ToListAsync();

                // Check to see if any product banners have been deleted
                foreach (ProductBanner dbProductBanner in dbProductBanners)
                {
                    if (!product.ProductBanners.Select(x => x.Name).ToList().Contains(dbProductBanner.Name))
                    {
                        db.ProductBanners.Remove(dbProductBanner);
                        ImageController.DeleteImageFile(dbProductBanner.Name);
                    }
                }

                // Check to see if any product videos have been deleted
                foreach (ProductVideo dbProductVideo in dbProductVideos)
                {
                    if (!product.ProductVideos.Select(x => x.Url).ToList().Contains(dbProductVideo.Url))
                    {
                        db.ProductVideos.Remove(dbProductVideo);
                    }
                }

                // Check to see if any product filters have been deleted
                foreach (ProductFilter dbProductFilter in dbProductFilters)
                {
                    if (!product.ProductFilters.Select(x => x.ID).ToList().Contains(dbProductFilter.ID))
                    {
                        db.ProductFilters.Remove(dbProductFilter);
                    }
                }

                // Check to see if any product banners need to be added or have been modified
                foreach (ProductBanner productBanner in product.ProductBanners)
                {
                    if (!(dbProductBanners.Count(x => x.Name == productBanner.Name) > 0))
                    {
                        db.Entry(productBanner).State = EntityState.Added;
                    }
                    else
                    {
                        ProductBanner dbProductBanner = dbProductBanners.FirstOrDefault(x => x.Name == productBanner.Name);
                        db.Entry(dbProductBanner).State = EntityState.Detached;
                        if (dbProductBanner.Selected != productBanner.Selected)
                        {
                            db.Entry(productBanner).State = EntityState.Modified;
                        }
                    }
                }

                // Check to see if any product videos need to be added
                foreach (ProductVideo productVideo in product.ProductVideos)
                {
                    if (!(dbProductVideos.Count(x => x.Url == productVideo.Url) > 0))
                    {
                        db.Entry(productVideo).State = EntityState.Added;
                    }
                }

                // Check to see if any product filters need to be added
                foreach (ProductFilter productFilter in product.ProductFilters)
                {
                    if (!(dbProductFilters.Count(x => x.ID == productFilter.ID) > 0))
                    {
                        db.Entry(productFilter).State = EntityState.Added;
                    }
                }

                // Check to see if this product has been modified
                Product dbProduct = db.Products.FirstOrDefault(x => x.ID == product.ID);
                db.Entry(dbProduct).State = EntityState.Detached;

                if (dbProduct.Name != product.Name || dbProduct.HopLink != product.HopLink || dbProduct.Description != product.Description || dbProduct.Image != product.Image || dbProduct.Price != product.Price || dbProduct.Featured != product.Featured)
                {
                    db.Entry(product).State = EntityState.Modified;
                }
            }

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }

            return Ok();
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

        //POST: api/Products
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> PostProduct(Product[] products)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (Product product in products)
            {
                db.Products.Add(product);
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Products/5
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> DeleteProduct(string itemIds)
        {
            string[] ids = itemIds.Split(',');

            foreach (string id in ids)
            {
                Product product = await db.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                // List of images to delete from the images directory
                List<string> imagesToDelete = new List<string>();

                // Add product images to the list
                if (product.Image != null) imagesToDelete.Add(product.Image);
                product.ProductBanners.ToList().ForEach(y => imagesToDelete.Add(y.Name));

                // Delete the images
                imagesToDelete.ForEach(x => ImageController.DeleteImageFile(x));

                // Remove this product from the database
                db.Products.Remove(product);
            }

            await db.SaveChangesAsync();
            return Ok();
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

    public class ProductGroup
    {
        public string caption;
        public List<ProductData> products;
    }

    public class ProductData
    {
        public string id;
        public string name;
        public string hopLink;
        public string description;
        public string image;
        public double price;
        public List<string> videos;
    }
}