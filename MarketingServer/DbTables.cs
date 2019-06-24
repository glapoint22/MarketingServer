using System.Data.Entity;

namespace MarketingServer
{
    public class DbTables
    {
        public static Category[] categories;
        public static PriceRange[] priceRanges;
        public static Filter[] filterList;
        public static ProductFilter[] productFilters;

        public async static void Set()
        {
            MarketingEntities db = new MarketingEntities();

            categories = await db.Categories.AsNoTracking().ToArrayAsync();
            priceRanges = await db.PriceRanges.AsNoTracking().ToArrayAsync();
            filterList = await db.Filters.AsNoTracking().ToArrayAsync();
            productFilters = await db.ProductFilters.AsNoTracking().ToArrayAsync();
        }
    }
}