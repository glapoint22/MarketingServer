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
                    .Where(c => SqlFunctions.DateDiff("day", c.EmailSentDate, DateTime.Today) >= c.EmailSendFrequency && c.Subscriptions
                        .Where(x => x.Subscribed && !x.Suspended).Count() != 0)
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
                        Email email;
                        Campaign newCampaign;

                        //Get the current campaign this customer is on
                        Campaign currentCampaign = await db.Campaigns
                            .Where(x => x.SubscriptionID == subscription.ID && !x.ProductPurchased && !x.Ended)
                            .FirstOrDefaultAsync();

                        if (currentCampaign == null)
                        {
                            //Error!
                            continue;
                        }

                        //Get the next email from this campaign
                        email = await GetEmail(currentCampaign.ProductID, currentCampaign.Day + 1);

                        /*
                        If the email is null, this means we are at the end of the 
                        campaign and must start a new campaign with a new product
                        */
                        if (email == null)
                        {
                            //Mark that the current campaign has ended
                            currentCampaign.Ended = true;
                            db.Entry(currentCampaign).State = EntityState.Modified;

                            //Get a new product
                            var productId = await GetNewProductId(subscription.NicheID, subscription.ID);

                            /*
                            If the product id is null, this means there are no more products in this 
                            subscription and we must suspend this subscription until more products are added
                            */
                            if(productId == null)
                            {
                                //Suspending subscription
                                subscription.Suspended = true;
                                db.Entry(subscription).State = EntityState.Modified;
                                continue;
                            }


                            //Start a new campaign with this new product
                            newCampaign = new Campaign
                            {
                                SubscriptionID = subscription.ID,
                                Date = DateTime.Now,
                                ProductID = productId,
                                Day = 1,
                            };
                            

                            //Get the first email from this campaign
                            email = await GetEmail(newCampaign.ProductID, newCampaign.Day);
                        }else
                        {
                            newCampaign = new Campaign
                            {
                                SubscriptionID = subscription.ID,
                                Date = DateTime.Now,
                                ProductID = currentCampaign.ProductID,
                                Day = currentCampaign.Day + 1,
                            };
                        }

                        Mail mail = new Mail(email.id, customer, email.subject, email.body);
                        //mail.Send();
                        customer.EmailSentDate = DateTime.Today;

                        //Add the new record
                        db.Campaigns.Add(newCampaign);
















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

        private async Task<Email> GetEmail(string productId, int day)
        {
            return await db.EmailCampaigns
                .Where(x => x.ProductID == productId && x.Day == day)
                .Select(x => new Email
                {
                    id = x.ID,
                    subject = x.Subject,
                    body = x.Body
                })
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
                .FirstOrDefaultAsync();
        }
    }
}
public class Email
{
    public string id;
    public string subject;
    public string body;
}