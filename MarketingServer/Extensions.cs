using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MarketingServer
{
    public static class Extensions
    {
        public static IQueryable<Product> Where(this IQueryable<Product> query, MarketingEntities db, string[] searchWords, int category, string[] languages, string[] productTypes, string[] billingTypes, int nicheId)
        {
            return db.Products.Where(x =>
                x.Active &&
                (nicheId > 0 ? x.NicheID == nicheId : true) &&
                searchWords.All(z => x.Name.Contains(z)) && 
                (category != 0 ? x.Nich.CategoryID == category : true) &&
                (languages.Count() > 0 ? 
                    (languages.Contains("English") ? x.English == true: true) && 
                    (languages.Contains("German") ? x.German == true : true) &&
                    (languages.Contains("Spanish") ? x.Spanish == true : true) &&
                    (languages.Contains("French") ? x.French == true : true) &&
                    (languages.Contains("Italian") ? x.Italian == true : true) &&
                    (languages.Contains("Portuguese") ? x.Portuguese == true : true)
                : true) &&
                 (productTypes.Count() > 0 ?
                    (productTypes.Contains("Digital Downlaod") ? x.DigitalDownload == true : true) &&
                    (productTypes.Contains("Shippable") ? x.Shippable == true : true)
                : true) &&
                (billingTypes.Count() > 0 ?
                    (billingTypes.Contains("Single Payment") ? x.SinglePayment == true : true) &&
                    (billingTypes.Contains("Subscription") ? x.Subscription == true : true) &&
                    (billingTypes.Contains("Trial") ? x.Trial == true : true)
                : true)
                );
        }
    }
}