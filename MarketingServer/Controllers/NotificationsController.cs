using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Infrastructure;
using System.Data;
using System.Data.Entity;

namespace MarketingServer.Controllers
{
    public class NotificationsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        public async Task<IHttpActionResult> Post(Notification notification)
        {
            //Decrypt the data
            string data = Encryption.Decrypt(notification.iv, notification.notification);

            //If the data is null, return bad request
            if(data == null)
            {
                //todo - send email
                return BadRequest();
            }

            //Parse the data
            JObject jObject = JObject.Parse(data);


            //If this notification has tracking codes
            if (jObject["trackingCodes"] != null)
            {
                //Get a list of all the tracking codes
                List<string> trackingCodes = jObject["trackingCodes"].ToObject<List<string>>();

                //Iterate through each tracking code
                foreach (string trackingCode in trackingCodes)
                {
                    //The length of the tracking code must be 20(10 for customer id and 10 for product id)
                    if(trackingCode.Length == 20)
                    {
                        string customerId = trackingCode.Substring(0, 10);
                        string productId = trackingCode.Substring(10);

                        //Check to see if the customer id and product id exists
                        if (await db.Customers.FindAsync(customerId) == null || await db.Products.FindAsync(productId) == null)
                        {
                            continue;
                        }


                        //Check to see if this campaign record exists based on the product id and the customer id
                        CampaignRecord campaignRecord = await db.CampaignRecords
                            .Where(x => db.Subscriptions
                                .Where(y => y.CustomerID == customerId)
                                .Select(y => y.ID)
                                .ToList().Contains(x.SubscriptionID)
                            && x.ProductID == productId)
                            .FirstOrDefaultAsync();

                        //If this campaign record is in the database
                        if (campaignRecord != null)
                        {
                            //Mark this product as purchased
                            campaignRecord.ProductPurchased = true;

                            //Get a new product we can email to the customer
                            string newProduct = await Campaign.GetProduct(campaignRecord.SubscriptionID);


                            if (newProduct != null)
                            {
                                //Add the new record
                                db.CampaignRecords.Add(Campaign.CreateCampaignRecord(campaignRecord.SubscriptionID, newProduct, false));
                            }
                            else
                            {
                                //There are no more products available. Test to see if we can suspend the subscription
                                bool isRecords = await db.CampaignRecords.AnyAsync(x => x.SubscriptionID == campaignRecord.SubscriptionID && x.ProductID != productId && !x.ProductPurchased);
                                if (!isRecords)
                                {
                                    campaignRecord.Subscription.Suspended = true;
                                }
                            }
                        }
                        //This campaign record does not exist in the database
                        else
                        {

                            int nicheId = await db.Products.Where(x => x.ID == productId).Select(x => x.NicheID).SingleAsync();
                            string subscriptionId = await db.Subscriptions.Where(x => x.CustomerID == customerId && x.NicheID == nicheId).Select(x => x.ID).SingleOrDefaultAsync();

                            //If this product is part of a new subscription, create a new subscription
                            if (subscriptionId == null)
                            {
                                Subscription subscription = SubscriptionsController.CreateSubscription(customerId, nicheId);
                                subscriptionId = subscription.ID;
                                db.Subscriptions.Add(subscription);
                            }

                            //Add the new record
                            db.CampaignRecords.Add(Campaign.CreateCampaignRecord(subscriptionId, productId, true));
                        }
                    }
                }
            }

           
            //If there are any changes, update the database
            if (db.ChangeTracker.HasChanges())
            {
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    throw;
                }
            }


            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}

public struct Notification
{
    public string notification;
    public string iv;
}
