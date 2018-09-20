using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace EmailService
{
    public partial class Service1 : ServiceBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private MarketingEntities db = new MarketingEntities();

        public Service1()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => SendEmails(_cancellationTokenSource.Token));
        }

        protected override void OnStop()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task SendEmails(CancellationToken token)
        {
            while (true)
            {
                //Calculate the minutes when we send emails
                DateTime date1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 11, 0, 0, 0);
                DateTime date2 = DateTime.Now;
                double minutes = date1.Subtract(date2).TotalMinutes;

                //If minutes is a negative number, that means we are passed the time. Add a day and calculte the minutes
                if (minutes < 0)
                {
                    date1 = date1.AddDays(1);
                    minutes = date1.Subtract(date2).TotalMinutes;
                }

                await Task.Delay(TimeSpan.FromMinutes(minutes), token);


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
                        string productId;

                        //Get the current campaign record from this subscription
                        CampaignRecord currentCampaignRecord = await db.CampaignRecords
                            .OrderByDescending(x => x.Date)
                            .Where(x => x.SubscriptionID == subscription.ID && !x.ProductPurchased && !x.Ended)
                            .FirstOrDefaultAsync();

                        productId = currentCampaignRecord.ProductID;

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
                            string newProduct = await GetProduct(subscription);

                            /*
                            If the product is null, this means there are no more products in this 
                            subscription and we must suspend this subscription until more products are added
                            */
                            if (newProduct == null)
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

                            productId = newCampaignRecord.ProductID;

                            //Get the first email from this campaign
                            email = await GetEmail(newCampaignRecord.ProductID, newCampaignRecord.Day);

                            if (email == null)
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

                        Mail mail = new Mail(email.id, customer, productId, email.subject, email.body);
                        await mail.Send();
                        customer.EmailSentDate = DateTime.Today;

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

        public async Task<string> GetProduct(Subscription subscription)
        {
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
    }
}

public class Email
{
    public string id;
    public string subject;
    public string body;
}
