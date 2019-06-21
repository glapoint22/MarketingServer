using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace MarketingServer
{
    public class DbTables
    {
        public static Category[] categories;
        public static Nich[] niches;
        public static PriceRange[] priceRanges;
        public static Filter[] filterList;
        public static ProductFilter[] productFilters;
        public static QueriedProduct[] queriedProducts;
        //public static Product[] products;

        public async static void Set()
        {
            MarketingEntities db = new MarketingEntities();

            categories = await db.Categories.AsNoTracking().ToArrayAsync();
            niches = await db.Niches.AsNoTracking().ToArrayAsync();
            priceRanges = await db.PriceRanges.AsNoTracking().ToArrayAsync();
            filterList = await db.Filters.AsNoTracking().ToArrayAsync();
            productFilters = await db.ProductFilters.AsNoTracking().ToArrayAsync();
            Product[] products = await db.Products.AsNoTracking().ToArrayAsync();

            queriedProducts = products.Select(x => new QueriedProduct
            {
                id = x.ID,
                name = x.Name,
                image = x.Image,
                price = x.Price,
                nicheId = x.NicheID,
                categoryId = x.Nich.CategoryID
            }).ToArray();
        }
    }
}