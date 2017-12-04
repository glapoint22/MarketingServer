using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MarketingServer
{
    public static class Extensions
    {
        public static IQueryable<Product> Where(this IQueryable<Product> query, MarketingEntities db, string[] searchWords, int category)
        {
            return db.Products.Where(x => searchWords.All(z => x.Name.Contains(z)) && (category != 0 ? x.Nich.CategoryID == category : true) && x.Active);
        }
    }
}