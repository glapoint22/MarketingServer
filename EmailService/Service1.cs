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
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;

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
        private Token accessToken;
        private Token refreshToken;
        private AuthenticationHeaderValue header;

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
            HttpStatusCode statusCode;

            SetTokens(await Login());

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
                        statusCode = await PostAsync("Mail", SerializeObject(new CampaignEmail(currentCampaignRecord.ProductID, currentCampaignRecord.Day + 1, customer)));

                        /*
                        If the email is not found, this means we are at the end of the 
                        email campaign and must start a new campaign with a new product
                        */
                        if (statusCode == HttpStatusCode.NotFound)
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
                            statusCode = await PostAsync("Mail", SerializeObject(new CampaignEmail(newCampaignRecord.ProductID, newCampaignRecord.Day, customer)));


                            if (statusCode == HttpStatusCode.NotFound)
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

                        statusCode = await PostAsync("CampaignRecords", SerializeObject(newCampaignRecord));
                    }
                }
            }
        }

        public async Task<List<T>> GetListAsync<T>(string uri)
        {
            await ValidateToken();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = header;
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

            await ValidateToken();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = header;
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


        public async Task<HttpStatusCode> PostAsync(string uri, StringContent content)
        {
            await ValidateToken();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = header;
                using (HttpResponseMessage response = await client.PostAsync(apiUrl + uri, content))
                {
                    return response.StatusCode;
                }
            }
        }


        public async Task<HttpStatusCode> PutAsync(string uri, StringContent content)
        {
            await ValidateToken();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = header;
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

        private void SetTokens(string response)
        {
            response = Regex.Replace(response, @"\.", "");

            JObject jObject = JObject.Parse(response);

            accessToken = new Token()
            {
                id = (string)jObject.SelectToken("access_token"),
                expires = (DateTime)jObject.SelectToken("expires")
            };

            refreshToken = new Token()
            {
                id = (string)jObject.SelectToken("refresh_token"),
                expires = (DateTime)jObject.SelectToken("refreshTokenExpires")
            };

            refreshToken.expires = refreshToken.expires.AddHours(-5);
            header = new AuthenticationHeaderValue("Bearer", accessToken.id);
        }

        private async Task ValidateToken()
        {
            if (accessToken.expires.Subtract(DateTime.Now).TotalMilliseconds < 60000)
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.PostAsync(apiUrl + "Token", new StringContent("grant_type=refresh_token&refresh_token=" +
                    refreshToken.id + "&client_id=" +
                    clientId + "&client_secret=" +
                    clientSecret)))
                    {
                        using (HttpContent httpContent = response.Content)
                        {
                            SetTokens(await httpContent.ReadAsStringAsync());
                        }
                    }
                }
            }
        }

        private async Task<string> Login()
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.PostAsync(apiUrl + "Token", new StringContent("username=" +
                    userName + "&password=" +
                    password + "&grant_type=password&client_id=" +
                    clientId + "&client_secret=" +
                    clientSecret)))
                {
                    using (HttpContent httpContent = response.Content)
                    {
                        return await httpContent.ReadAsStringAsync();
                    }
                }
            }
        }
    }
}