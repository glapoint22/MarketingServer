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

        public static IQueryable<Product> Where(this IQueryable<Product> source, QueryParams queryParams)
        {

            char separator = '^';

            //Search words
            if (queryParams.searchWords != string.Empty)
            {
                string[] searchWordsArray = queryParams.searchWords.Split(' ');
                source = source.Where(x => searchWordsArray.Any(z => x.Name.Contains(z)));
            }


            //Category
            if (queryParams.categoryId >= 0)
            {
                source = source.Where(x => x.Nich.CategoryID == queryParams.categoryId);
            }


            //Niche
            if (queryParams.nicheId >= 0)
            {
                source = source.Where(x => x.NicheID == queryParams.nicheId);
            }


            //Filters
            if (queryParams.filters.Length > 0)
            {
                Match result;

                //Price Filter
                result = queryParams.filters.GetFilter("Price");
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

                    for (int i = 0; i < priceRangeArray.Length; i++)
                    {
                        string price = priceRangeArray[i];

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

                    for (int i = 0; i < priceRangeList.Count(); i++)
                    {
                        PriceRange priceRange = priceRangeList[i];
                        PriceRange temp = priceRange;
                        predicate = predicate.Or(x => x.Price >= temp.Min && x.Price < temp.Max);
                    }

                    source = source.Where(predicate);
                }


                //Custom Filters
                for (int i = 0; i < DbTables.filterList.Length; i++)
                {
                    Filter currentFilter = DbTables.filterList[i];
                    result = queryParams.filters.GetFilter(currentFilter.Name);


                    if (result.Length > 0)
                    {
                        //Get the options chosen from this filter
                        string[] optionsArray = result.Groups[2].Value.Split(separator);

                        //Get a list of ids from the options array
                        int[] optionIdList = currentFilter.FilterLabels.Where(x => optionsArray.Contains(x.Name)).Select(x => x.ID).ToArray();

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


            return source;
        }
    }
}