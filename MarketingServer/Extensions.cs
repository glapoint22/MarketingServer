using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;

namespace MarketingServer
{
    public static class Extensions
    {
        public static IOrderedQueryable<Product> OrderBy(this IQueryable<Product> source, string sort, string query)
        {
            IOrderedQueryable<Product> sortResult = null;

            switch (sort)
            {
                case "relevance":
                    sortResult = source.OrderBy(x => x.Name.StartsWith(query) ? (x.Name == query ? 0 : 1) : 2);
                    break;
                case "price-asc":
                    sortResult = source.OrderBy(x => x.Price);
                    break;
                case "price-desc":
                    sortResult = source.OrderByDescending(x => x.Price);
                    break;
            }

            return sortResult;
        }
    }
}