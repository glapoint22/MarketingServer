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
using System.Net.Http.Headers;
using System.Net.Http;
using System.Runtime.Caching;

namespace MarketingServer
{
    public class ProductsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Products
        [ResponseType(typeof(Product))]
        [AllowAnonymous]
        public async Task<IHttpActionResult> GetProducts()
        {
            string sessionId;
            string customerId = null;
            string productIds = null;

            sessionId = Session.GetSessionID(Request.Headers);

            if (sessionId != null) customerId = await db.Customers.AsNoTracking().Where(x => x.SessionID == sessionId).Select(x => x.ID).FirstOrDefaultAsync();

            CookieHeaderValue cookie = Request.Headers.GetCookies("Products").FirstOrDefault();
            if (cookie != null)
            {
                productIds = cookie["Products"].Value;
            }


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

        public async Task<IHttpActionResult> GetProduct(int nicheId, string subscriptionId)
        {
            string productId = await db.Products
                .AsNoTracking()
                .Where(x => x.NicheID == nicheId
                        && !x.CampaignRecords
                            .Where(z => z.SubscriptionID == subscriptionId)
                            .Select(z => z.ProductID)
                            .ToList()
                            .Contains(x.ID))
                .Select(x => x.ID)
                .FirstOrDefaultAsync();

            return Ok(productId);
        }

        private async Task<ProductGroup> GetFeaturedProducts(string customerId)
        {
            return new ProductGroup
            {
                caption = "Check out our featured products",
                products = await db.Products
                        .AsNoTracking()
                        .Where(x => x.Featured)
                        .OrderBy(x => x.Name)
                        .Select(z => new ProductData
                        {
                            id = z.ID,
                            name = z.Name,
                            hopLink = z.HopLink + (customerId != null ? (z.HopLink.IndexOf("?") == -1 ? "?" : "&") + "tid=" + customerId + z.ID : ""),
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
                .AsNoTracking()
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
                            hopLink = z.HopLink + (z.HopLink.IndexOf("?") == -1 ? "?" : "&") + "tid=" + customerId + z.ID,
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
                .AsNoTracking()
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
                .AsNoTracking()
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
                    hopLink = x.HopLink + (customerId != null ? (x.HopLink.IndexOf("?") == -1 ? "?" : "&") + "tid=" + customerId + x.ID : ""),
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

        public static string GetRegExPattern(string filterName)
        {
            return "(" + filterName + "\\|)([a-zA-Z0-9`~!@#$%^&*()\\-_+={[}\\]\\:;\"\'<,>.?/\\s]+)";
        }

        private List<FilterData> GetFilters(string searchWords, int category, int nicheId, string queryFilters, QueriedProduct[] queriedProducts)
        {
            List<FilterData> filters = new List<FilterData>();
            List<Label> labels;
            FilterData filter;
            string exclude = "";
            //IEnumerable<dynamic> queriedProducts;

            //Create labels for the price filter
            labels = new List<Label>();
            //if (Regex.Match(queryFilters, GetRegExPattern("Price")).Length > 0)
            //{
            //    exclude = "Price";
            //    queriedProducts = await db.Products.Where(searchWords, category, nicheId, queryFilters, exclude)
            //    .AsNoTracking()
            //    .Where(searchWords, category, nicheId, queryFilters, exclude)
            //    .Select(x => new
            //    {
            //        price = x.Price,
            //    }).ToListAsync();
            //}
            //else
            //{
            //queriedProducts = products;
            //}




            foreach (PriceRange priceRange in DbTables.priceRanges)
            {
                bool count = db.Products.Any(x => x.Price >= priceRange.Min && x.Price < priceRange.Max);
                if (count)
                {
                    //if (count != products.Count() || exclude == "Price")
                    //{
                    Label label = new Label
                    {
                        name = priceRange.Label,
                        //productCount = count
                    };
                    labels.Add(label);
                    //}
                }
            }

            if (labels.Count > 0)
            {
                //Create the price filter
                filter = new FilterData
                {
                    caption = "Price",
                    labels = labels
                };

                filters.Add(filter);
            }




            foreach (Filter currentFilter in DbTables.filterList)
            {
                //Create the labels for the current filter
                labels = new List<Label>();

                exclude = "";
                //if (Regex.Match(queryFilters, GetRegExPattern(currentFilter.Name)).Length > 0)
                //{
                //    exclude = currentFilter.Name;
                //    queriedProducts = await db.Products.Where(searchWords, category, nicheId, queryFilters, exclude)
                //    .AsNoTracking()
                //    .Where(searchWords, category, nicheId, queryFilters, exclude)
                //    .Select(x => new
                //    {
                //        id = x.ID,
                //    }).ToListAsync();
                //}
                //else
                //{
                //queriedProducts = products;
                //}


                foreach (FilterLabel filterLabel in currentFilter.FilterLabels)
                {
                    bool count = db.Products.Any(x => x.ProductFilters
                        .Where(z => z.FilterLabelID == filterLabel.ID)
                        .Select(z => z.ProductID)
                        .ToList()
                        .Contains(x.ID)
                    );
                    //If the count is greater than zero, create the label
                    if (count)
                    {
                        //if (count != products.Count() || exclude == currentFilter.Name)
                        //{
                        Label label = new Label
                        {
                            name = filterLabel.Name,
                            //productCount = count,
                        };
                        labels.Add(label);
                        //}


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

        [AllowAnonymous]
        public IHttpActionResult GetProductsFromSearch(string sort, int limit, int category = -1, string query = "", int nicheId = -1, int page = 1, string filter = "")
        {
            int currentPage;
            //string sessionId;
            //string customerId = null;

            //sessionId = Session.GetSessionID(Request.Headers);

            //if (sessionId != null) customerId = await db.Customers.AsNoTracking().Where(x => x.SessionID == sessionId).Select(x => x.ID).FirstOrDefaultAsync();

            QueriedProduct[] queriedProducts = GetQueriedProducts(query, category, nicheId, filter);
            int[] productNiches = queriedProducts.Select(p => p.nicheId).Distinct().ToArray();
            int[] productCategories = queriedProducts.Select(p => p.categoryId).Distinct().ToArray();

            var data = new
            {
                totalProducts = queriedProducts.Length,
                page = currentPage = page > 0 && page <= Math.Ceiling((double)queriedProducts.Length / limit) ? page : 1,
                products = queriedProducts
                            .OrderBy(sort, query)
                            .ThenBy(x => x.name)
                            .Skip((currentPage - 1) * limit)
                            .Take(limit)
                            .Select(x => new
                            {
                                name = x.name,
                                image = x.image,
                                price = x.price
                            })
                            .ToArray(),
                categories = DbTables.categories
                .Where(x => productCategories.Contains(x.ID))
                .Select(a => new
                {
                    id = a.ID,
                    name = a.Name,
                    niches = a.Niches.Where(w => productNiches.Contains(w.ID))
                    .Select(c => new
                    {
                        id = c.ID,
                        name = c.Name
                    }).ToArray()
                }).ToArray(),
                filters = GetFilters(query, category, nicheId, filter, queriedProducts)
            };

            return Ok(data);
        }


        private QueriedProduct[] GetQueriedProducts(string searchWords, int category, int nicheId, string filter)
        {
            string key = searchWords + category + nicheId + filter;

            QueriedProduct[] queriedProducts = Caching.Get<QueriedProduct[]>(key);



            if (queriedProducts == null)
            {
                // No cache to grab from
                if (   (searchWords.Length > 0 && category == -1 && nicheId == -1 && filter.Length == 0)  || (searchWords.Length == 0 && category >= 0 && nicheId == -1 && filter.Length == 0))
                {
                    return QueryProducts(queriedProducts, searchWords, category, nicheId, filter, key);
                }


                // Grab cache from just search words
                else if (searchWords.Length > 0 && category >= 0 && nicheId == -1 && filter.Length == 0)
                {
                    return QueryProducts(Caching.Get<QueriedProduct[]>(searchWords + "-1" + nicheId + filter), searchWords, category, nicheId, filter, key);
                }


                // Grab cache from just category
                else if (searchWords.Length == 0 && category >= 0 && nicheId >= 0 && filter.Length == 0)
                {
                    return QueryProducts(Caching.Get<QueriedProduct[]>(searchWords + category + "-1" + filter), searchWords, category, nicheId, filter, key);

                }


                // Grab cache from category then try search words
                else if (searchWords.Length > 0 && category >= 0 && nicheId >= 0 && filter.Length == 0)
                {
                    queriedProducts = Caching.Get<QueriedProduct[]>(searchWords + category + "-1" + filter);

                    if (queriedProducts == null)
                    {
                        return QueryProducts(Caching.Get<QueriedProduct[]>(searchWords + "-1-1" + filter), searchWords, category, nicheId, filter, key);
                    }
                    else
                    {
                        return QueryProducts(queriedProducts, searchWords, category, nicheId, filter, key);
                    }
                }



                else if (searchWords.Length > 0 && category == -1 && nicheId == -1 && filter.Length > 0)
                {

                    var result = Regex.Match(filter, @"([\w\s]+)(?:\|)");

                    return QueryProducts(queriedProducts, searchWords, category, nicheId, filter, key);
                }







                else if (searchWords.Length > 0 && category >= 0 && nicheId == -1 && filter.Length > 0)
                {

                }
                else if (searchWords.Length == 0 && category >= 0 && nicheId == -1 && filter.Length > 0)
                {

                }
                else if (searchWords.Length == 0 && category >= 0 && nicheId >= 0 && filter.Length > 0)
                {

                }
                else if (searchWords.Length > 0 && category >= 0 && nicheId >= 0 && filter.Length > 0)
                {

                }

            }



            return queriedProducts;
        }


        private QueriedProduct[] QueryProducts(QueriedProduct[] queriedProducts, string query, int category, int nicheId, string filter, string key)
        {
            if (queriedProducts == null)
            {
                queriedProducts = DbTables.queriedProducts
                .Where(query, category, nicheId, filter)
                .ToArray();
            }
            else
            {
                queriedProducts = queriedProducts
                .Where(query, category, nicheId, filter)
                .ToArray();
            }

            Caching.Add(key, queriedProducts);

            return queriedProducts;
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
                if (product.Name != null)
                {
                    // Get a list of product banners, product videos, and product filters for this product
                    List<ProductBanner> dbProductBanners = await db.ProductBanners.AsNoTracking().Where(x => x.ProductID == product.ID).ToListAsync();
                    List<ProductVideo> dbProductVideos = await db.ProductVideos.AsNoTracking().Where(x => x.ProductID == product.ID).ToListAsync();
                    List<ProductFilter> dbProductFilters = await db.ProductFilters.AsNoTracking().Where(x => x.ProductID == product.ID).ToListAsync();


                    // Check to see if any product banners have been deleted
                    foreach (ProductBanner dbProductBanner in dbProductBanners)
                    {
                        if (!product.ProductBanners.Select(x => x.Name).ToList().Contains(dbProductBanner.Name))
                        {
                            db.ProductBanners.Attach(dbProductBanner);
                            db.ProductBanners.Remove(dbProductBanner);
                            ImageController.DeleteImageFile(dbProductBanner.Name);
                        }
                    }

                    // Check to see if any product videos have been deleted
                    foreach (ProductVideo dbProductVideo in dbProductVideos)
                    {
                        if (!product.ProductVideos.Select(x => x.Url).ToList().Contains(dbProductVideo.Url))
                        {
                            db.ProductVideos.Attach(dbProductVideo);
                            db.ProductVideos.Remove(dbProductVideo);
                        }
                    }

                    // Check to see if any product filters have been deleted
                    foreach (ProductFilter dbProductFilter in dbProductFilters)
                    {
                        if (!product.ProductFilters.Select(x => x.ID).ToList().Contains(dbProductFilter.ID))
                        {
                            db.ProductFilters.Attach(dbProductFilter);
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
                    Product dbProduct = db.Products.AsNoTracking().FirstOrDefault(x => x.ID == product.ID);


                    if (dbProduct.Name != product.Name || dbProduct.HopLink != product.HopLink || dbProduct.Description != product.Description || dbProduct.Image != product.Image || dbProduct.Price != product.Price || dbProduct.Featured != product.Featured)
                    {
                        db.Entry(product).State = EntityState.Modified;
                    }
                }
                else
                {
                    List<EmailCampaign> dbEmailCampaigns = await db.EmailCampaigns.AsNoTracking().Where(x => x.ProductID == product.ID).ToListAsync();

                    // Check to see if any emails have been deleted
                    foreach (EmailCampaign dbEmailCampaign in dbEmailCampaigns)
                    {
                        if (!product.EmailCampaigns.Select(x => x.ID).ToList().Contains(dbEmailCampaign.ID))
                        {
                            db.EmailCampaigns.Attach(dbEmailCampaign);
                            db.EmailCampaigns.Remove(dbEmailCampaign);

                        }
                    }

                    // Check to see if any email campaigns need to be added or have been modified
                    foreach (EmailCampaign emailCampaign in product.EmailCampaigns)
                    {
                        if (!(dbEmailCampaigns.Count(x => x.ID == emailCampaign.ID) > 0))
                        {
                            db.Entry(emailCampaign).State = EntityState.Added;
                        }
                        else
                        {
                            EmailCampaign dbEmailCampaign = dbEmailCampaigns.FirstOrDefault(x => x.ID == emailCampaign.ID);
                            if (dbEmailCampaign.Subject != emailCampaign.Subject || dbEmailCampaign.Body != emailCampaign.Body || dbEmailCampaign.Day != emailCampaign.Day)
                            {
                                db.Entry(emailCampaign).State = EntityState.Modified;
                            }
                        }
                    }
                }

            }

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }

            return Ok();
        }

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
    }


    public struct Label
    {
        public string name;
        public int productCount;
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

    public struct QueriedProduct
    {
        public string id;
        public string name;
        public string image;
        public double price;
        public int categoryId;
        public int nicheId;
    }
}



public class Caching
{
    private static readonly ObjectCache Cache = MemoryCache.Default;

    public static T Get<T>(string key) where T : class
    {
        try
        {
            return (T)Cache[key];
        }
        catch (Exception)
        {

            return null;
        }
    }

    public static void Add<T>(string key, T item) where T : class
    {
        Cache.Add(key, item, DateTimeOffset.Now.AddHours(1));
    }
}