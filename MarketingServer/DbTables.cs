using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace MarketingServer
{
    public class DbTables
    {
        public static List<Category> categories;
        public static List<Nich> niches;
        public static List<PriceRange> priceRanges;
        public static List<Filter> filterList;
        public static List<ProductFilter> productFilters;
        public static IEnumerable<Product> products;

        public async static void Set()
        {
            MarketingEntities db = new MarketingEntities();

            categories = await db.Categories.AsNoTracking().ToListAsync();
            niches = await db.Niches.AsNoTracking().ToListAsync();
            priceRanges = await db.PriceRanges.AsNoTracking().ToListAsync();
            filterList = await db.Filters.AsNoTracking().ToListAsync();
            productFilters = await db.ProductFilters.AsNoTracking().ToListAsync();
            products = await db.Products.AsNoTracking().ToListAsync();
        }
    }
}