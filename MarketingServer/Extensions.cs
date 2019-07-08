using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MarketingServer
{
    public static class Extensions
    {
        public static IOrderedQueryable<Product> OrderBy(this IQueryable<Product> source, QueryParams queryParams)
        {
            IOrderedQueryable<Product> sortResult = null;

            switch (queryParams.sort)
            {
                case "relevance":
                    sortResult = source.OrderBy(x => x.Name.StartsWith(queryParams.searchWords) ? (x.Name == queryParams.searchWords ? 0 : 1) : 2);
                    break;
                case "price-asc":
                    sortResult = source.OrderBy(x => x.Price);
                    break;
                case "price-desc":
                    sortResult = source.OrderByDescending(x => x.Price);
                    break;
                default:
                    sortResult = source.OrderBy(x => x.Price);
                    break;
            }

            return sortResult;
        }

        public static IQueryable<Product> Where(this IQueryable<Product> productsQuery, QueryParams queryParams)
        {
            //Search words
            if (queryParams.searchWords != string.Empty)
            {
                string[] searchWordsArray = queryParams.searchWords.Split(' ');
                productsQuery = productsQuery.Where(x => searchWordsArray.Any(z => x.Name.ToLower().Contains(z.ToLower())));
            }


            //Category
            if (queryParams.categoryId >= 0)
            {
                productsQuery = productsQuery.Where(x => x.Nich.CategoryID == queryParams.categoryId);
            }


            //Niche
            if (queryParams.nicheId >= 0)
            {
                productsQuery = productsQuery.Where(x => x.NicheID == queryParams.nicheId);
            }


            //Filters
            if (queryParams.filters.Length > 0)
            {
                //Price Filter
                if (queryParams.filters.Contains("Price"))
                {
                    PriceRangeOption priceRange = queryParams.filters.GetPriceRange();

                    if(priceRange.min == priceRange.max)
                    {
                        productsQuery = productsQuery.Where(x => x.Price == priceRange.min);
                    }
                    else
                    {
                        productsQuery = productsQuery.Where(x => x.Price >= priceRange.min && x.Price < priceRange.max);
                    }
                }


                // Custom filters
                List<SelectedFilterOptions> selectedFilterOptions = queryParams.filters.GetSelectedOptions();

                // Include the selected filters in the query
                for (int i = 0; i < selectedFilterOptions.Count(); i++)
                {
                    int[] selectedOptions = selectedFilterOptions[i].options;
                    productsQuery = productsQuery
                        .Where(x => x.ProductFilters
                            .Where(z => selectedOptions.Contains(z.FilterLabelID))
                            .Select(z => z.ProductID)
                            .ToList()
                            .Contains(x.ID)
                        );
                }
            }

            return productsQuery;
        }
    }
}