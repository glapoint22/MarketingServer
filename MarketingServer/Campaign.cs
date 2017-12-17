using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Data;
using System.Data.Entity;

namespace MarketingServer
{
    public class Campaign
    {
        public static async Task<string> GetProduct(Subscription subscription)
        {
            MarketingEntities db = new MarketingEntities();

            return await db.Products
                .OrderBy(x => x.Order)
                .Where(x => x.NicheID == subscription.NicheID
                        && !x.CampaignRecords
                            .Where(z => z.SubscriptionID == subscription.ID)
                            .Select(z => z.ProductID)
                            .ToList()
                            .Contains(x.ID))
                .Select(x => x.ID)
                .FirstOrDefaultAsync();
        }

        public static async Task<CampaignRecord> CreateCampaignRecord(Subscription subscription)
        {
            MarketingEntities db = new MarketingEntities();

            return new CampaignRecord
            {
                SubscriptionID = subscription.ID,
                Date = DateTime.Now,
                ProductID = await GetProduct(subscription),
                Day = 0
            };
        }

        public static CampaignRecord CreateCampaignRecord(string subscriptionId, string productId, bool isPurchased)
        {
            return new CampaignRecord
            {
                SubscriptionID = subscriptionId,
                Date = DateTime.Now,
                ProductID = productId,
                Day = 0,
                ProductPurchased = isPurchased
            };
        }
    }
}