using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Threading.Tasks;
using System.Data;
using System.Data.Entity;
using System;
using System.Data.Entity.SqlServer;



namespace MarketingServer
{
    public class WebApiApplication : HttpApplication
    {
        private MarketingEntities db = new MarketingEntities();

        protected async void Application_Start()
        {

            GlobalConfiguration.Configure(WebApiConfig.Register);
            
            await Run();
        }

        public async Task Run()
        {
            //TimeSpan span = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 28, 0) - DateTime.Now;

            //DateTime dateTime = DateTime.Parse("8:23 PM");


            while (true)
            {
                //await Task.Delay(5000);

                //Get a list of customers that we will be sending emails to
                List<Customer> customers = await db.Customers
                    .Where(c => c.Subscriptions
                        .Where(x => x.Subscribed && !x.Suspended).Count() != 0 && SqlFunctions.DateDiff("day", db.Campaigns
                            .Where(e => e.Subscription.CustomerID == c.ID)
                            .OrderByDescending(x => x.Date)
                            .Select(x => x.Date)
                            .FirstOrDefault(), DateTime.Today) >= c.EmailSendFrequency)
                    .AsNoTracking()
                    .ToListAsync();

                
                //Iterate through each customer
                foreach (Customer customer in customers)
                {
                    //Get a list of subscriptions this customer is subscribed to
                    List<Subscription> subscriptions = customer.Subscriptions
                        .Where(c => c.Subscribed && !c.Suspended)
                        .Select(c => c)
                        .ToList();

                    //Iterate through each subscription
                    foreach (Subscription subscription in subscriptions)
                    {
                        object email;

                        //Get the current campaign this customer is on
                        var currentCampaign = await db.Campaigns
                            .OrderByDescending(x => x.Date)
                            .Where(x => x.SubscriptionID == subscription.ID)
                            .Select(x => new {
                                productId = x.ProductID,
                                day = x.Day,
                                productPurchased = x.ProductPurchased
                            })
                            .FirstOrDefaultAsync();


                        if (!currentCampaign.productPurchased)
                        {
                            email = await GetEmail(currentCampaign.productId, currentCampaign.day + 1);
                            if(email == null)
                            {
                                //Get a new product
                                var productId = await GetNewProductId(subscription.NicheID, subscription.ID);

                                //Start a new campaign with this new product
                                Campaign newCampaign = new Campaign
                                {
                                    SubscriptionID = subscription.ID,
                                    Date = DateTime.Today,
                                    ProductID = productId,
                                    Day = 1,
                                };
                            }
                        }
                        else
                        {
                            //Get another product
                        }


                            
                        
                        










                        //Advance to the next day of this campaign
                        //var campaign = await db.Campaigns.Where(x => x.SubscriptionID == subscription.ID).OrderByDescending(x => x.Date).Select(x => new 
                        //{
                        //    productId = x.ProductID,
                        //    day = x.Day
                        //}).AsNoTracking().FirstOrDefaultAsync();

                        //Set up a new log for this campaign
                        //Campaign campaignLog = new Campaign
                        //{
                        //    SubscriptionID = subscription.ID,
                        //    Date = DateTime.Today,
                        //    ProductID = campaign.productId,
                        //    Day = campaign.day,
                        //    CustomerID = customer.ID
                        //};


                        //See if this campaign actually exists
                        //bool exists = await db.EmailCampaigns.AnyAsync(x => x.ProductID == campaign.productId && x.Day == campaign.day);

                        //if (!exists)
                        //{
                        //The current campaign does not exists, so get another campaign in this niche 
                        //int nextproductId = await db.Products.Where(c => c.NicheID == subscription.NicheID && c.ID > campaign.productId).OrderBy(x => x.ID).Select(c => c.ID).FirstOrDefaultAsync();

                        //If there are no other campaigns in this niche, suspend this subscription
                        //if (nextproductId == 0)
                        //{
                        //    subscription.Suspended = true;
                        //    db.Entry(subscription).State = EntityState.Modified;
                        //    continue;
                        //}
                        //else
                        //{
                        //    //Set the new campaign
                        //    campaignLog.ProductID = nextproductId;
                        //    campaignLog.Day = 1;
                        //}
                        // }

                        //Get the email and send
                        //var email = await db.Emails.Where(x => x.ProductID == campaignLog.ProductID && x.Day == campaignLog.Day).Select(x => new {
                        //    id = x.ID,
                        //    subject = x.Subject,
                        //    body = x.Body
                        //}).AsNoTracking().FirstOrDefaultAsync();

                        //Mail mail = new Mail(email.id, customer, email.subject, email.body);
                        //mail.Send();

                        //Log this campaign
                        //db.CampaignLogs.Add(campaignLog);
                    }
                }

                //Update the database
                if (db.ChangeTracker.HasChanges())
                {
                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch
                    {
                        throw;
                    }
                    
                }
            }
            
        }

        private async Task<object> GetEmail(string productId, int day)
        {
            return await db.EmailCampaigns
                .Where(x => x.ProductID == productId && x.Day == day)
                .Select(x => new
                {
                    id = x.ID,
                    subject = x.Subject,
                    body = x.Body
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        private async Task<string> GetNewProductId(int nicheId, int subscriptionId)
        {
            return await db.Products
                .Where(x => x.NicheID == nicheId && !x.Campaigns
                    .Where(z => z.SubscriptionID == subscriptionId)
                    .Select(z => z.ProductID)
                    .ToList()
                    .Contains(x.ID))
                .Select(x => x.ID)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }
    }
}
