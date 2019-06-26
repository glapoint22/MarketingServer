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

        [AllowAnonymous]
        public async Task<IHttpActionResult> GetProduct(string name)
        {
            var product = await db.Products.Where(x => x.Name.ToLower() == name).Select(x => x.Name).FirstOrDefaultAsync();

            return Ok(product);
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


        [AllowAnonymous]
        public async Task<IHttpActionResult> GetProductsFromSearch(string sort, int limit, int category = -1, string query = "", int nicheId = -1, int page = 1, string filter = "")
        {
            int currentPage;
            QueryParams queryParams = new QueryParams(query, category, nicheId, sort, filter);

            // Get the key
            string key = queryParams.GetKey();

            // First try finding the cached products from the key
            CachedProduct[] cachedProducts = Caching.Get<CachedProduct[]>(key);

            // If there is no cache, then grab the products from the database
            if (cachedProducts == null) cachedProducts = await QueryProducts(queryParams, key);

            // Get arrays of categories and niches from these products 
            int[] productCategories = cachedProducts.Select(p => p.categoryId).Distinct().ToArray();
            int[] productNiches = cachedProducts.Select(p => p.nicheId).Distinct().ToArray();
            

            var data = new
            {
                totalProducts = cachedProducts.Length,
                page = currentPage = page > 0 && page <= Math.Ceiling((double)cachedProducts.Length / limit) ? page : 1,
                products = cachedProducts
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
                filters = await GetFilters(cachedProducts, queryParams)
            };

            return Ok(data);
        }

        private async Task<List<FilterData>> GetFilters(CachedProduct[] cachedProducts, QueryParams queryParams)
        {
            List<FilterData> filters = new List<FilterData>();
            List<Label> labels;
            FilterData filter;
            CachedProduct[] products;
            labels = new List<Label>();

            // If the query params contains the price filter
            if (queryParams.filters.Contains("Price"))
            {
                // Temporarily remove the price filter so we can query products without the price
                QueryParams tempParams = queryParams;
                tempParams.filters.RemoveFilter("Price");

                // See if we have cached products
                string key = tempParams.GetKey();
                products = Caching.Get<CachedProduct[]>(key);

                // If no cache, get the products from the database
                if (products == null)
                {
                    products = await QueryProducts(tempParams, key);
                }
            }
            else
            {
                // Grab the products from cache
                products = cachedProducts;
            }


            for (int i = 0; i < DbTables.priceRanges.Length; i++)
            {
                PriceRange priceRange = DbTables.priceRanges[i];

                // Do the products contain this price range? If so, create a label
                if (products.Any(x => x.price >= priceRange.Min && x.price < priceRange.Max))
                {
                    Label label = new Label
                    {
                        name = priceRange.Label,
                    };
                    labels.Add(label);
                }
            }

            // If we have any labels, create the price filter
            if (labels.Count > 0)
            {
                filter = new FilterData
                {
                    caption = "Price",
                    labels = labels
                };

                filters.Add(filter);
            }


            // Iterate through each filter
            for (int i = 0; i < DbTables.filterList.Length; i++)
            {
                Filter currentFilter = DbTables.filterList[i];
                labels = new List<Label>();


                // If the query params contains the current filter
                if (queryParams.filters.Contains(currentFilter.Name))
                {
                    // Temporarily remove this filter so we can query products without it
                    QueryParams tempParams = queryParams;
                    tempParams.filters.RemoveFilter(currentFilter.Name);

                    // See if we have cached products
                    string key = tempParams.GetKey();
                    products = Caching.Get<CachedProduct[]>(key);

                    if (products == null)
                    {
                        products = await QueryProducts(tempParams, key);
                    }
                }
                else
                {
                    products = cachedProducts;
                }

                // Iterate through each label
                FilterLabel[] filterLabels = currentFilter.FilterLabels.ToArray();
                for (int index = 0; index < filterLabels.Length; index++)
                {
                    FilterLabel filterLabel = filterLabels[index];

                    // Create a dictionary of all products using this filter option for fast lookup
                    Dictionary<string, string> productsDictionary = DbTables.productFilters
                        .Where(z => z.FilterLabelID == filterLabel.ID)
                        .Select(z => z.ProductID).ToDictionary(x => x);


                    // Loop through each product from the query and see if it is in the dictionary of products
                    for (int j = 0; j < products.Length; j++)
                    {
                        // See if the dictionary contains this product. If so, this means this product is using this filter option
                        if (productsDictionary.ContainsKey(products[j].id))
                        {
                            Label label = new Label
                            {
                                name = filterLabel.Name,
                            };
                            labels.Add(label);
                            break;
                        }
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


        private async Task<CachedProduct[]> QueryProducts(QueryParams queryParams, string key)
        {
            var products = await db.Products
                .Where(queryParams)
                .OrderBy(queryParams)
                .ThenBy(x => x.Name)
                .Select(x => new 
                {
                    id = x.ID,
                    name = x.Name,
                    image = x.Image,
                    price = x.Price,
                    nicheId = x.NicheID,
                    categoryId = x.Nich.CategoryID
                }).ToArrayAsync();

            CachedProduct[] cachedProducts = products
                .Select(x => new CachedProduct
                {
                    id = x.id,
                    name = x.name,
                    image = x.image,
                    price = x.price,
                    nicheId = x.nicheId,
                    categoryId = x.categoryId
                }).ToArray();


            Caching.Add(key, cachedProducts);

            return cachedProducts;
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

    public struct CachedProduct
    {
        public string id;
        public string name;
        public string image;
        public double price;
        public int categoryId;
        public int nicheId;
    }

    public struct QueryParams
    {
        public string searchWords;
        public int categoryId;
        public int nicheId;
        public string sort;
        public QueryFilters filters;

        public QueryParams(string searchWords, int categoryId, int nicheId, string sort, string filters)
        {
            this.searchWords = searchWords;
            this.categoryId = categoryId;
            this.nicheId = nicheId;
            this.sort = sort;
            this.filters = new QueryFilters(filters);
        }

        public string GetKey()
        {
            List<string> paramList = new List<string>();
            paramList.Add(searchWords);
            paramList.Add(categoryId.ToString());
            paramList.Add(nicheId.ToString());
            paramList.AddRange(filters.GetFilterList());

            // Sort the param list
            string[] sorted = paramList.OrderBy(x => x).ToArray();
            return string.Join("", sorted);
        }
    }


    public struct QueryFilters
    {
        private string filters;

        public int Length
        {
            get
            {
                return filters.Length;
            }
        }

        private string GetPattern(string filter)
        {
            return "(" + filter + @")\|([$0-9a-zA-Z\-^\s\[\]]+)\|";
        }

        public QueryFilters(string filters)
        {
            this.filters = filters;
        }

        public Match GetFilter(string filter)
        {
            return Regex.Match(filters, GetPattern(filter));
        }

        public List<string> GetFilterList()
        {
            // Create a list of the filters
            List<string> filterList = Regex.Matches(filters, GetPattern(@"[\w\s]+")).Cast<Match>().Select(x => x.Value).ToList();


            for(int i = 0; i < filterList.Count; i++)
            {
                // Get a list of all the options for this filter
                List<string> filterOptionList = Regex.Matches(filterList[i], @"([$0-9a-zA-Z\-\s\[\]]+)").Cast<Match>().Select(x => x.Value).ToList();

                // Remove the first index which is the option name
                string optionName = filterOptionList[0];
                filterOptionList.Remove(filterOptionList[0]);

                // Order the options
                List<string> ordered = filterOptionList.OrderBy(x => x).ToList();

                // Piece together the current filter with the sorted options
                filterList[i] = optionName + "|";
                for(int j = 0; j < ordered.Count; j++)
                {
                    filterList[i] += ordered[j];
                    if (j != ordered.Count - 1) filterList[i] += "^";
                }
                filterList[i] += "|";
            }

            return filterList;
        }

        public bool Contains(string filter)
        {
            return GetFilter(filter).Length > 0;
        }

        public void RemoveFilter(string filter)
        {
            filters = Regex.Replace(filters, GetPattern(filter), "");
        }
    }
}