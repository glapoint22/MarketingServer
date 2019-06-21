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
        public static IOrderedEnumerable<QueriedProduct> OrderBy(this IEnumerable<QueriedProduct> source, string sort, string query)
        {
            IOrderedEnumerable<QueriedProduct> sortResult = null;

            switch (sort)
            {
                case "relevance":
                    sortResult = source.OrderBy(x => x.name.StartsWith(query) ? (x.name == query ? 0 : 1) : 2);
                    break;
                case "price-asc":
                    sortResult = source.OrderBy(x => x.price);
                    break;
                case "price-desc":
                    sortResult = source.OrderByDescending(x => x.price);
                    break;
                default:
                    sortResult = source.OrderBy(x => x.price);
                    break;
            }

            return sortResult;
        }

        public static IEnumerable<QueriedProduct> Where(this IEnumerable<QueriedProduct> source, string searchWords, int category, int nicheId, string queryFilters, string filterExclude = "")
        {
            
            char separator = '^';

            //Search words
            if (searchWords != string.Empty)
            {
                string[] searchWordsArray = searchWords.Split(' ');
                source = source.Where(x => searchWordsArray.Any(z => x.name.Contains(z)));
            }


            //Category
            if (category > -1)
            {
                source = source.Where(x => x.categoryId == category);
            }


            //Niche
            if (nicheId > -1)
            {
                source = source.Where(x => x.nicheId == nicheId);
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

                        for(int i = 0; i < priceRangeArray.Length; i++)
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

                        Expression<Func<QueriedProduct, bool>> predicate = ExpressionBuilder.False<QueriedProduct>();

                        for(int i = 0; i < priceRangeList.Count(); i++)
                        {
                            PriceRange priceRange = priceRangeList[i];
                            PriceRange temp = priceRange;
                            predicate = predicate.Or(x => x.price >= temp.Min && x.price < temp.Max);
                        }

                        source = source.Where(predicate.Compile());
                    }
                }



                //Custom Filters
                for(int i = 0; i < DbTables.filterList.Length; i++)
                {
                    Filter currentFilter = DbTables.filterList[i];

                    if (filterExclude != currentFilter.Name)
                    {
                        result = Regex.Match(queryFilters, ProductsController.GetRegExPattern(currentFilter.Name));

                        if (result.Length > 0)
                        {
                            //Get the options chosen from this filter
                            string[] optionsArray = result.Groups[2].Value.Split(separator);

                            //Get a list of ids from the options array
                            int[] optionIdList = currentFilter.FilterLabels.Where(x => optionsArray.Contains(x.Name)).Select(x => x.ID).ToArray();

                            //Set the query
                            source = source
                                .Where(x => DbTables.productFilters
                                    .Where(z => optionIdList.Contains(z.FilterLabelID))
                                    .Select(z => z.ProductID)
                                    .ToList()
                                    .Contains(x.id)
                                );
                        }
                    }
                }
            }


            return source;
        }
    }
}