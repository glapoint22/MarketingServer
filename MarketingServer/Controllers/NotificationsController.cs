using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Text;
using System.Data;
using System.Data.Entity;

namespace MarketingServer.Controllers
{
    public class NotificationsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        public async Task<IHttpActionResult> Post(Notification notification)
        {
            //notification.iv = "NTI2QUZEMkFFODg4N0NDMg==";
            //notification.notification = "eEcIz5RKE35BI8dyBXae+t6EnFxRyojDQXtG7bB0YukVpPqRK3jXSp2HTu7oWFZq2GSgH5PVNUeY7pk8AfmwbRkfIqcLiyKf6nV3K64JE2WGHVvGxbY0qiZAkPYx+CqwfK/tMWNq/FXk9Fy7SdbXBj2ViDOAWEZiFDNuWTKNfFSazuPyIyxM+E0S7N/Son8me85I/zz82Sc0MALlhYMEswj5IwcwD4OlEGhpiKB1FS016YEXndH0sdbipDXy9bOLTWedi4Do3fbckvV6EbJi/PDl/h5zSVAwi+wYFh6Gq3+hJYTBdrFGyIs+JpGEo1Li4yfOH2s+vcYFN0msOmBZr0rv3QZ7uu63LbLhCNNBqNjotpM2ZFASFFftIXKv1LK4AdBkaeJEp91P2VJCcVDeaMHwacB8lHtzqOH+9IFxmqa3RygriCq1tZ2r8gLYq819GxVU4W9kbwgNkcjO4/4FPYvsun9YXW4EKxBAY1nF3ggoUFXt3jsG/oLcQwm7u7aDvR94a9EbaP8L0qrTMHDvUtnCzDley/OQ66FDWG2uASp+5RWNNCApMmzWwxtL7QV6pI+QtfQtpgoZ1+F8GQ5tEe5L2CC3LetpAYCW1cL7ayyhw6iTK5QF3rM+I/DQXhWQtlJywFnjRoG5lQhLDEP0rpKT/mvvtaZt1tloDXDUKANInDjThmsFFhjhmZsGqGru4OrSW88iTnVDruwYQlEYQg==";

            string data = Encryption.Decrypt(notification.iv, notification.notification);

            if(data == null)
            {
                //todo - send email
                return BadRequest();
            }
                

            //content = "{'transactionTime':'2016-06-05T13:47:51-06:00','receipt':'CWOGBZLN','transactionType':'SALE','vendor':'testacct','affiliate':'affiliate1','role':'VENDOR','totalAccountAmount':0.00,'paymentMethod':'VISA','totalOrderAmount':0.00,'totalTaxAmount':0.00,'totalShippingAmount':0.00,'currency':'USD','orderLanguage':'EN','trackingCodes':['68cc94ecca199', '68cc94ecca5'],'lineItems':[{'itemNo':'1','productTitle':'ProductTitle','shippable':false,'recurring':false,'accountAmount':5.00,'quantity':1,'downloadUrl':'<download_url>','lineItemType':'CART'},{'itemNo':'2','productTitle':'SecondProduct','shippable':false,'recurring':true,'accountAmount':2.99,'quantity':1,'downloadUrl':'<download_url>','lineItemType':'CART'}],'customer':{'shipping':{'firstName':'TEST','lastName':'GUY','fullName':'TestGuy','phoneNumber':'','email':'glapoint22@gmail.com','address':{'address1':'12TestLane','address2':'Suite100','city':'LASVEGAS','county':'LASVEGAS','state':'NV','postalCode':'89101','country':'US'}},'billing':{'firstName':'TEST','lastName':'GUY','fullName':'TestGuy','phoneNumber':'','email':'glapoint22@gmail.com','address':{'state':'NV','postalCode':'89101','country':'US'}}},'upsell':{'upsellOriginalReceipt':'CWOGBZLN','upsellFlowId':55,'upsellSession':'VVVVVVVVVV','upsellPath':'upsell_path'},'version':6.0,'attemptCount':1,'vendorVariables':{'v1':'variable1','v2':'variable2'}}";

            JObject jObject = JObject.Parse(data);



            if (jObject["trackingCodes"] != null)
            {
                List<string> trackingCodes = jObject["trackingCodes"].ToObject<List<string>>();

                foreach (string trackingCode in trackingCodes)
                {
                    string customerId = trackingCode.Substring(0, 10);
                    string productId = trackingCode.Substring(10);

                    if(await db.Customers.FindAsync(customerId) == null || await db.Products.FindAsync(productId) == null)
                    {
                        continue;
                    }

                    CampaignRecord campaignRecord = await db.CampaignRecords
                        .Where(x => x.SubscriptionID == x.Subscription.ID && x.ProductID == productId)
                        .OrderByDescending(x => x.Date)
                        .FirstOrDefaultAsync();


                    if (campaignRecord != null)
                    {
                        campaignRecord.ProductPurchased = true;

                        string newProductId = await Campaign.GetNewProductId(campaignRecord.Subscription.NicheID, campaignRecord.SubscriptionID);

                        if (newProductId != null)
                        {
                            //Add the new record
                            db.CampaignRecords.Add(Campaign.CreateCampaignRecord(campaignRecord.SubscriptionID, newProductId, false));
                        }
                        else
                        {
                            bool isRecords = await db.CampaignRecords.AnyAsync(x => x.SubscriptionID == campaignRecord.SubscriptionID && !x.ProductPurchased);
                            if (!isRecords)
                            {
                                campaignRecord.Subscription.Suspended = true;
                            }
                        }
                    }
                    else
                    {
                        int nicheId = await db.Products.Where(x => x.ID == productId).Select(x => x.NicheID).SingleAsync();
                        string subscriptionId = await db.Subscriptions.Where(x => x.CustomerID == customerId && x.NicheID == nicheId).Select(x => x.ID).SingleOrDefaultAsync();

                        if(subscriptionId == null)
                        {
                            Subscription subscription = SubscriptionsController.CreateSubscription(customerId, nicheId);
                            subscriptionId = subscription.ID;
                            db.Subscriptions.Add(subscription);
                        }

                        
                        db.CampaignRecords.Add(Campaign.CreateCampaignRecord(subscriptionId, productId, true));
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
