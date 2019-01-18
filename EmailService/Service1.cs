using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MarketingServer;
using System.Text;
using System.Net;

namespace EmailService
{
    public partial class Service1 : ServiceBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        //private MarketingEntities db = new MarketingEntities();
        private string apiUrl = "http://localhost:49699/api/";

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

        public async Task<List<T>> GetListAsync<T>(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(apiUrl + uri))
                {
                    using (HttpContent content = response.Content)
                    {
                        string contentString = await content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<T>>(contentString);

                    }
                }
            }
        }

        public async Task<T> GetAsync<T>(string uri, params string[][] args)
        {
            string parameters = string.Empty;

            for(int i = 0; i < args.Length; i++)
            {
                parameters += (i == 0) ? "?" : "&";
                parameters += args[i][0] + "=" + args[i][1];
            }


            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(apiUrl + uri + parameters))
                {
                    using (HttpContent content = response.Content)
                    {
                        string contentString = await content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<T>(contentString);

                    }
                }
            }
        }

        //public async Task<HttpStatusCode> PostMailAsync(CampaignEmail campaignEmail)
        //{
        //    using (HttpClient client = new HttpClient())
        //    {

        //        StringContent content = new StringContent(JsonConvert.SerializeObject(campaignEmail), Encoding.UTF8, "application/json");

        //        using (HttpResponseMessage response = await client.PostAsync(apiUrl + "Mail", content))
        //        {
        //            return response.StatusCode;
                    
        //        }
        //    }
        //}


        public async Task<HttpStatusCode> PostAsync(string uri, object obj)
        {
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await client.PostAsync(apiUrl + uri, content))
                {
                    return response.StatusCode;
                }
            }
        }


        public async Task<HttpStatusCode> PutAsync(string uri, object obj)
        {
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await client.PutAsync(apiUrl + uri, content))
                {
                    return response.StatusCode;
                }
            }
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

                //await Task.Delay(TimeSpan.FromMinutes(minutes), token);



                //Get a list of customers that we will be sending emails to
                List<Customer> customers = await GetListAsync<Customer>("Customers");

                //Iterate through each customer
                foreach (Customer customer in customers)
                {
                    //Get a list of subscriptions this customer is subscribed to
                    List<Subscription> subscriptions = customer.Subscriptions
                        .Where(c => c.Subscribed && !c.Suspended)
                        .ToList();

                    //Iterate through each subscription
                    foreach (Subscription subscription in subscriptions)
                    {
                        CampaignRecord newCampaignRecord;

                        //Get the current campaign record from this subscription
                        CampaignRecord currentCampaignRecord = await GetAsync<CampaignRecord>("CampaignRecords",  new[] { "subscriptionID", subscription.ID });

                        if (currentCampaignRecord == null)
                        {
                            //Error!
                            continue;
                        }

                        // Send out the email
                        HttpStatusCode status = await PostAsync("Mail", new CampaignEmail(currentCampaignRecord.ProductID, currentCampaignRecord.Day + 1, customer));

                        /*
                        If the email is not found, this means we are at the end of the 
                        email campaign and must start a new campaign with a new product
                        */
                        if (status == HttpStatusCode.NotFound)
                        {
                            //Mark the current campaign record that this campaign has ended
                            currentCampaignRecord.Ended = true;

                            if(await PutAsync("CampaignRecords", currentCampaignRecord) == HttpStatusCode.InternalServerError)
                            {
                                // Error!
                                continue;
                            }

                            //Get a new product id
                            string newProductId = await GetAsync<string>("Products", new[] { "nicheId", subscription.NicheID.ToString() }, new[] { "subscriptionId", subscription.ID });

                            /*
                            If the product id is null, this means there are no more products in this 
                            subscription and we must suspend this subscription until more products are added
                            */
                            if (newProductId == null)
                            {
                                //Suspending subscription
                                subscription.Suspended = true;

                                if (await PutAsync("Subscriptions", subscription) == HttpStatusCode.InternalServerError)
                                {
                                    // Error!
                                }

                                continue;
                            }

                            //Start a new campaign with this new product
                            newCampaignRecord = new CampaignRecord
                            {
                                SubscriptionID = subscription.ID,
                                Date = DateTime.Now,
                                ProductID = newProductId,
                                Day = 1,
                            };


                            //Send out the first email from this campaign
                            status = await PostAsync("Mail",  new CampaignEmail(newCampaignRecord.ProductID, newCampaignRecord.Day, customer));


                            if (status == HttpStatusCode.NotFound)
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

                        

                        //Mail mail = new Mail(email.id, customer, email.subject, email.body, await Mail.GetRelatedProducts(subscription.NicheID, email.id, customer.ID, productId));
                        //await mail.Send();
                        //customer.EmailSentDate = DateTime.Today;



                        //Add the new record
                        //db.CampaignRecords.Add(newCampaignRecord);
                    }
                }

                //Update the database
                //if (db.ChangeTracker.HasChanges())
                //{
                //    try
                //    {
                //        await db.SaveChangesAsync();
                //    }
                //    catch
                //    {
                //        throw;
                //    }
                //}
            }
        }

        //private async Task<Email> GetEmail(string productId, int day)
        //{
        //    return await db.EmailCampaigns
        //        .Where(x => x.ProductID == productId && x.Day == day)
        //        .Select(x => new Email
        //        {
        //            id = x.ID,
        //            subject = x.Subject,
        //            body = x.Body
        //        })
        //        .FirstOrDefaultAsync();
        //}

        //public async Task<string> GetProduct(Subscription subscription)
        //{
        //    return await db.Products
        //        .OrderBy(x => x.Order)
        //        .Where(x => x.NicheID == subscription.NicheID
        //                && !x.CampaignRecords
        //                    .Where(z => z.SubscriptionID == subscription.ID)
        //                    .Select(z => z.ProductID)
        //                    .ToList()
        //                    .Contains(x.ID))
        //        .Select(x => x.ID)
        //        .FirstOrDefaultAsync();
        //}
    }
}

//public class foo
//{
//    public string id;
//    public string subject;
//    public string body;
//}
