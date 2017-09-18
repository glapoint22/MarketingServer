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
            
            //await Run();
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
                    .Where(c => c.Subscriptions.Where(x => x.Subscribed && !x.Suspended).Count() != 0
                    && SqlFunctions.DateDiff("day", c.CampaignLogs.Where(e => e.CustomerID == c.ID).OrderByDescending(x => x.Date).Select(x => x.Date).FirstOrDefault(), DateTime.Today) >= c.EmailSendFrequency
                    )
                    .Select(c => c).AsNoTracking().ToListAsync();

                
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
                        //Advance to the next day of this campaign
                        var campaign = await db.CampaignLogs.Where(x => x.SubscriptionID == subscription.ID).OrderByDescending(x => x.Date).Select(x => new 
                        {
                            campaignId = x.CampaignID,
                            day = x.Day + 1
                        }).AsNoTracking().FirstOrDefaultAsync();

                        //Set up a new log for this campaign
                        CampaignLog campaignLog = new CampaignLog
                        {
                            SubscriptionID = subscription.ID,
                            Date = DateTime.Today,
                            CampaignID = campaign.campaignId,
                            Day = campaign.day,
                            CustomerID = customer.ID
                        };


                        //See if this campaign actually exists
                        bool exists = await db.Emails.AnyAsync(x => x.CampaignID == campaign.campaignId && x.Day == campaign.day);
                        
                        if (!exists)
                        {
                            //The current campaign does not exists, so get another campaign in this niche 
                            int nextCampaignId = await db.Campaigns.Where(c => c.NicheID == subscription.NicheID && c.ID > campaign.campaignId).OrderBy(x => x.ID).Select(c => c.ID).FirstOrDefaultAsync();

                            //If there are no other campaigns in this niche, suspend this subscription
                            if (nextCampaignId == 0)
                            {
                                subscription.Suspended = true;
                                db.Entry(subscription).State = EntityState.Modified;
                                continue;
                            }
                            else
                            {
                                //Set the new campaign
                                campaignLog.CampaignID = nextCampaignId;
                                campaignLog.Day = 1;
                            }
                        }

                        //Get the email and send
                        var email = await db.Emails.Where(x => x.CampaignID == campaignLog.CampaignID && x.Day == campaignLog.Day).Select(x => new {
                            id = x.ID,
                            subject = x.Subject,
                            body = x.Body
                        }).AsNoTracking().FirstOrDefaultAsync();

                        Mail mail = new Mail(email.id, customer, email.subject, email.body);
                        //mail.Send();

                        //Log this campaign
                        db.CampaignLogs.Add(campaignLog);
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
    }
}
