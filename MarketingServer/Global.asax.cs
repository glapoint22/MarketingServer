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
                        CampaignRecord newCampaignRecord;

                        //Get the current campaign record from this subscription
                        CampaignRecord currentCampaignRecord = await db.CampaignRecords
                            .OrderByDescending(x => x.Date)
                            .Where(x => x.SubscriptionID == subscription.ID && !x.ProductPurchased && !x.Ended)
                            .FirstOrDefaultAsync();

                        if (currentCampaignRecord == null)
                        {
                            //Error!
                            continue;
                        }

                        //Get the next email from this campaign
                        email = await GetEmail(currentCampaignRecord.ProductID, currentCampaignRecord.Day + 1);

                        /*
                        If the email is null, this means we are at the end of the 
                        email campaign and must start a new campaign with a new product
                        */
                        if (email == null)
                        {
                            //Mark the current campaign record that this campaign has ended
                            currentCampaignRecord.Ended = true;

                            //Get a new product
                            string newProduct = await Campaign.GetProduct(subscription.NicheID, subscription.ID);

                            /*
                            If the product is null, this means there are no more products in this 
                            subscription and we must suspend this subscription until more products are added
                            */
                            if(newProduct == null)
                            {
                                //Suspending subscription
                                subscription.Suspended = true;
                                continue;
                            }

                            //Start a new campaign with this new product
                            newCampaignRecord = new CampaignRecord
                            {
                                SubscriptionID = subscription.ID,
                                Date = DateTime.Now,
                                ProductID = newProduct,
                                Day = 1,
                            };
                            

                            //Get the first email from this campaign
                            email = await GetEmail(newCampaignRecord.ProductID, newCampaignRecord.Day);

                            if(email == null)
                            {
                                //Error!
                                continue;
                            }
                        }
                        else
                        {
                            //Create a new record for this campaign
                            newCampaignRecord = new CampaignRecord
                            {
                                SubscriptionID = subscription.ID,
                                Date = DateTime.Now,
                                ProductID = currentCampaignRecord.ProductID,
                                Day = currentCampaignRecord.Day + 1,
                            };
                        }

                        Mail mail = new Mail(email.id, customer, email.subject, email.body);
                        //mail.Send();
                        //customer.EmailSentDate = DateTime.Today;

                        //Add the new record
                        db.CampaignRecords.Add(newCampaignRecord);
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
    }
}
public class Email
{
    public string id;
    public string subject;
    public string body;
}