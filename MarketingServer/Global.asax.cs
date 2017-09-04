using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Threading.Tasks;
using System.Net.Mail;
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
            Mail mail = new Mail();
            //TimeSpan span = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 28, 0) - DateTime.Now;

            //DateTime dateTime = DateTime.Parse("8:23 PM");


            while (true)
            {
                //await Task.Delay(5000);

                //Get a list of customers that we will be sending emails to
                List<Customer> customers = await db.Customers
                    .Where(c => c.Subscriptions.Where(x => x.Active && x.Subscribed).Count() != 0
                    && SqlFunctions.DateDiff("day", c.EmailSentDate, DateTime.Today) >= c.EmailSendFrequency
                    )
                    .Select(c => c).ToListAsync();

                //Iterate through each customer
                foreach (Customer customer in customers)
                {
                    //Get a list of subscriptions this customer is subscribed to
                    List<Subscription> subscriptions = customer.Subscriptions
                        .Where(c => c.Active && c.Subscribed)
                        .Select(c => c)
                        .ToList();

                    //Iterate through each subscription
                    foreach (Subscription subscription in subscriptions)
                    {
                        //Compose the email and send
                        var email = await db.Emails.Where(e => e.CampaignID == subscription.CurrentCampaignID && e.Day == subscription.CurrentEmailDay).Select(e => new
                        {
                            subject = e.Subject,
                            body = e.Body
                        }).AsNoTracking().SingleAsync();

                        mail.Send(customer.ID, email.subject, email.body);

                        //Get the next email day
                        int nextDay = await db.Emails.Where(e => e.Day > subscription.CurrentEmailDay && e.CampaignID == subscription.CurrentCampaignID)
                                    .Select(e => e.Day).FirstOrDefaultAsync();


                        //If there another email day for this campaign
                        if (nextDay != 0)
                        {
                            subscription.CurrentEmailDay = nextDay;
                        }
                        else
                        {
                            //There were no more emails for the current campaign, so lets grab another campaign id
                            int nextCampaignId = await db.Campaigns.Where(c => c.NicheID == subscription.NicheID && c.ID > subscription.CurrentCampaignID).Select(c => c.ID).FirstOrDefaultAsync();

                            //If there are no more campaigns left to this niche, set inactive
                            if (nextCampaignId == 0)
                            {
                                subscription.Active = false;
                                continue;
                            }

                            //Set the next campaign id and day
                            subscription.CurrentEmailDay = 1;
                            subscription.CurrentCampaignID = nextCampaignId;
                        }
                    }

                    //Mark the date when the email(s) has been sent
                    customer.EmailSentDate = DateTime.Today;

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
