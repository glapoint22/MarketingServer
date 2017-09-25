using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Infrastructure;

namespace MarketingServer.Controllers
{
    public class NotificationsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        public async Task<IHttpActionResult> Post(Notification notification)
        {
            string content = Encryption.Decrypt(notification.iv, notification.notification);

            //content = "{'transactionTime':'2016-06-05T13:47:51-06:00','receipt':'CWOGBZLN','transactionType':'SALE','vendor':'testacct','affiliate':'affiliate1','role':'VENDOR','totalAccountAmount':0.00,'paymentMethod':'VISA','totalOrderAmount':0.00,'totalTaxAmount':0.00,'totalShippingAmount':0.00,'currency':'USD','orderLanguage':'EN','trackingCodes':['tracking_code'],'lineItems':[{'itemNo':'1','productTitle':'ProductTitle','shippable':false,'recurring':false,'accountAmount':5.00,'quantity':1,'downloadUrl':'<download_url>','lineItemType':'CART'},{'itemNo':'2','productTitle':'SecondProduct','shippable':false,'recurring':true,'accountAmount':2.99,'quantity':1,'downloadUrl':'<download_url>','lineItemType':'CART'}],'customer':{'shipping':{'firstName':'TEST','lastName':'GUY','fullName':'TestGuy','phoneNumber':'','email':'glapoint22@gmail.com','address':{'address1':'12TestLane','address2':'Suite100','city':'LASVEGAS','county':'LASVEGAS','state':'NV','postalCode':'89101','country':'US'}},'billing':{'firstName':'TEST','lastName':'GUY','fullName':'TestGuy','phoneNumber':'','email':'glapoint22@gmail.com','address':{'state':'NV','postalCode':'89101','country':'US'}}},'upsell':{'upsellOriginalReceipt':'CWOGBZLN','upsellFlowId':55,'upsellSession':'VVVVVVVVVV','upsellPath':'upsell_path'},'version':6.0,'attemptCount':1,'vendorVariables':{'v1':'variable1','v2':'variable2'}}";

            JObject jObject = JObject.Parse(content);


            

            Transaction transaction = new Transaction
            {
                id = db.Transactions.Count() + 1,
                transactionTime = (DateTime)jObject.SelectToken("transactionTime"),
                receipt = (string)jObject.SelectToken("receipt"),
                transactionType = (string)jObject.SelectToken("transactionType"),
                vendor = (string)jObject.SelectToken("vendor"),
                affiliate = (string)jObject.SelectToken("affiliate"),
                role = (string)jObject.SelectToken("role"),
                totalAccountAmount = (double)jObject.SelectToken("totalAccountAmount"),
                paymentMethod = (string)jObject.SelectToken("paymentMethod"),
                totalOrderAmount = (double)jObject.SelectToken("totalOrderAmount"),
                totalTaxAmount = (double)jObject.SelectToken("totalTaxAmount"),
                totalShippingAmount = (double)jObject.SelectToken("totalShippingAmount"),
                currency = (string)jObject.SelectToken("currency"),
                orderLanguage = (string)jObject.SelectToken("orderLanguage"),
            };

            db.Transactions.Add(transaction);

            if (jObject["lineItems"] != null)
            {
                List<LineItem> lineItems = jObject.SelectToken("lineItems").Select(x => new LineItem
                {
                    transactionId = transaction.id,
                    itemNo = (string)x["itemNo"],
                    productTitle = (string)x["productTitle"],
                    shippable = (bool)x["shippable"],
                    recurring = (bool)x["recurring"],
                    accountAmount = (double)x["accountAmount"],
                    quantity = (int)x["quantity"],
                    downloadUrl = (string)x["downloadUrl"],
                    lineItemType = (string)x["lineItemType"]
                }).ToList();

                foreach(LineItem lineItem in lineItems)
                {
                    db.LineItems.Add(lineItem);
                }
            }


            if (jObject["upsell"] != null)
            {
                List<JProperty> list = jObject.SelectToken("upsell").Children<JProperty>().ToList();

                Upsell upsell = new Upsell
                {
                    upsellOriginalReceipt = (string)list.First(x => x.Name == "upsellOriginalReceipt").Value,
                    upsellFlowId = (int)list.First(x => x.Name == "upsellFlowId").Value,
                    upsellSession = (string)list.First(x => x.Name == "upsellSession").Value,
                    upsellPath = (string)list.First(x => x.Name == "upsellPath").Value
                };

                db.Upsells.Add(upsell);
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
