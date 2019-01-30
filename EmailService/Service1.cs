using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MarketingServer;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace EmailService
{
    public partial class Service1 : ServiceBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private string apiUrl = "http://localhost:49699/api/";
        private string userName = "email";
        private string password = "Z0r!0th22";
        private string clientId = "autoresponder";
        private string clientSecret = "As!rama1";

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
            string response;

            response = await PostAsync("Token", new StringContent("username=" +
              userName + "&password=" +
              password + "&grant_type=password&client_id=" +
              clientId + "&client_secret=" +
              clientSecret));

            

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
                        CampaignRecord currentCampaignRecord = await GetAsync<CampaignRecord>("CampaignRecords", new[] { "subscriptionID", subscription.ID });

                        if (currentCampaignRecord == null)
                        {
                            //Error!
                            continue;
                        }

                        // Send out the email
                        response = await PostAsync("Mail", SerializeObject(new CampaignEmail(currentCampaignRecord.ProductID, currentCampaignRecord.Day + 1, customer)));

                        /*
                        If the email is not found, this means we are at the end of the 
                        email campaign and must start a new campaign with a new product
                        */
                        if (response == "Not Found")
                        {
                            //Mark the current campaign record that this campaign has ended
                            currentCampaignRecord.Ended = true;

                            if (await PutAsync("CampaignRecords", SerializeObject(currentCampaignRecord)) == HttpStatusCode.InternalServerError)
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

                                if (await PutAsync("Subscriptions/V2", SerializeObject(subscription)) == HttpStatusCode.InternalServerError)
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
                            response = await PostAsync("Mail", SerializeObject(new CampaignEmail(newCampaignRecord.ProductID, newCampaignRecord.Day, customer)));


                            if (response == "Not Found")
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

                        response = await PostAsync("CampaignRecords", SerializeObject(newCampaignRecord));
                    }
                }
            }
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

            for (int i = 0; i < args.Length; i++)
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


        public async Task<string> PostAsync(string uri, StringContent content)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.PostAsync(apiUrl + uri, content))
                {
                    if (response.StatusCode == HttpStatusCode.NotFound) return "Not Found";
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }


        public async Task<HttpStatusCode> PutAsync(string uri, StringContent content)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.PutAsync(apiUrl + uri, content))
                {
                    return response.StatusCode;
                }
            }
        }

        private StringContent SerializeObject(object obj)
        {
            return new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
        }
    }
}