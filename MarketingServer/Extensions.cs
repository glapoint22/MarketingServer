using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

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
                default:
                    sortResult = source.OrderBy(x => x.Price);
                    break;
            }

            return sortResult;
        }

        public static IQueryable<Product> Where(this IQueryable<Product> source, string searchWords, int category, int nicheId, string queryFilters, string filterExclude = "")
        {
            
            char separator = '^';

            //Search words
            if (searchWords != string.Empty)
            {
                string[] searchWordsArray = searchWords.Split(' ');
                source = source.Where(x => searchWordsArray.Any(z => x.Name.Contains(z)));
            }


            //Category
            if (category > -1)
            {
                source = source.Where(x => x.Nich.CategoryID == category);
            }


            //Niche
            if (nicheId > -1)
            {
                source = source.Where(x => x.NicheID == nicheId);
            }


            //Filters
            if (queryFilters != string.Empty)
            {
                Match result;

                if (filterExclude != "Price")
                {
                    //Price Filter
                    result = Regex.Match(queryFilters, ProductsController.GetRegExPattern("Price"));
                    if (result.Length > 0)
                    {
                        string[] priceRangeArray = result.Groups[2].Value.Split(separator);
                        List<PriceRange> priceRangeList = DbTables.priceRanges
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
                            predicate = predicate.Or(x => x.Price >= temp.Min && x.Price < temp.Max);
                        }

                        source = source.Where(predicate);
                    }
                }



                //Custom Filters
                foreach (Filter currentFilter in DbTables.filterList)
                {
                    if (filterExclude != currentFilter.Name)
                    {
                        result = Regex.Match(queryFilters, ProductsController.GetRegExPattern(currentFilter.Name));

                        if (result.Length > 0)
                        {
                            //Get the options chosen from this filter
                            string[] optionsArray = result.Groups[2].Value.Split(separator);

                            //Get a list of ids from the options array
                            List<int> optionIdList = currentFilter.FilterLabels.Where(x => optionsArray.Contains(x.Name)).Select(x => x.ID).ToList();

                            //Set the query
                            source = source
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


            return source;
        }
    }
}