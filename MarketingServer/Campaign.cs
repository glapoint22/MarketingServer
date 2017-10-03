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
        public static async Task<string> GetNewProductId(int nicheId, string subscriptionId)
        {
            MarketingEntities db = new MarketingEntities();

            return await db.Products
                .Where(x => x.NicheID == nicheId && !x.CampaignRecords
                    .Where(z => z.SubscriptionID == subscriptionId)
                    .Select(z => z.ProductID)
                    .ToList()
                    .Contains(x.ID))
                .Select(x => x.ID)
                .FirstOrDefaultAsync();
        }

        public static async Task<CampaignRecord> CreateCampaignRecord(string subscriptionId, int nicheId)
        {
            MarketingEntities db = new MarketingEntities();

            return new CampaignRecord
            {
                SubscriptionID = subscriptionId,
                Date = DateTime.Now,
                ProductID = await db.Products
                    .Where(x => x.NicheID == nicheId)
                    .Select(x => x.ID).FirstOrDefaultAsync(),
                Day = 0,
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