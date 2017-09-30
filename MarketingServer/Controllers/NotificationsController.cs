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

            

            //if(jObject["trackingCodes"] != null)
            //{
            //    List<string> trackingCodes = jObject["trackingCodes"].ToObject<List<string>>();

            //    foreach(string trackingCode in trackingCodes)
            //    {
            //        string customerId = trackingCode.Substring(0, 10);
            //        string productId = trackingCode.Substring(10);
            //        Campaign campaign = await db.Campaigns.Where(x => x.CustomerID == customerId && x.ProductID == productId).OrderByDescending(x => x.Date).FirstOrDefaultAsync();


            //        if (campaign != null)
            //        {
            //            campaign.ProductPurchased = true;
            //            db.Entry(campaign).State = EntityState.Modified;
            //        }
            //    }
            //}
            

            //Transaction transaction = new Transaction
            //{
            //    id = Guid.NewGuid().ToString("N").Substring(0, 10),
            //    transactionTime = jObject["transactionTime"] != null ? (DateTime)jObject.SelectToken("transactionTime"):DateTime.Now,
            //    receipt = jObject["receipt"] != null ? (string)jObject.SelectToken("receipt"): "NA",
            //    transactionType = jObject["transactionType"] != null ? (string)jObject.SelectToken("transactionType"): "NA",
            //    vendor = jObject["vendor"] != null ? (string)jObject.SelectToken("vendor"): "NA",
            //    affiliate = jObject["affiliate"] != null ? (string)jObject.SelectToken("affiliate"): "NA",
            //    role = jObject["role"] != null ? (string)jObject.SelectToken("role"): "NA",
            //    totalAccountAmount = jObject["totalAccountAmount"] != null ? (double)jObject.SelectToken("totalAccountAmount"): 0,
            //    paymentMethod = jObject["paymentMethod"] != null ? (string)jObject.SelectToken("paymentMethod"): "NA",
            //    totalOrderAmount = jObject["totalOrderAmount"] != null ? (double)jObject.SelectToken("totalOrderAmount"): 0,
            //    totalTaxAmount = jObject["totalTaxAmount"] != null ? (double)jObject.SelectToken("totalTaxAmount"): 0,
            //    totalShippingAmount = jObject["totalShippingAmount"] != null ? (double)jObject.SelectToken("totalShippingAmount"): 0,
            //    currency = jObject["currency"] != null ? (string)jObject.SelectToken("currency"): "NA",
            //    orderLanguage = jObject["orderLanguage"] != null ? (string)jObject.SelectToken("orderLanguage"): "NA",
            //};

            //db.Transactions.Add(transaction);

            //if (jObject["lineItems"] != null)
            //{
            //    List<LineItem> lineItems = jObject.SelectToken("lineItems").Select(x => new LineItem
            //    {
            //        transactionId = transaction.id,
            //        itemNo = x["itemNo"] != null ? (string)x["itemNo"]: "NA",
            //        productTitle = x["productTitle"] != null ? (string)x["productTitle"]: "NA",
            //        shippable = x["shippable"] != null ? (bool)x["shippable"]: false,
            //        recurring = x["recurring"] != null ? (bool)x["recurring"]: false,
            //        accountAmount = x["accountAmount"] != null ? (double)x["accountAmount"]: 0,
            //        quantity = x["quantity"] != null ? (int)x["quantity"]: 0,
            //        lineItemType = x["lineItemType"] != null ? (string)x["lineItemType"]: "NA"
            //    }).ToList();

            //    foreach(LineItem lineItem in lineItems)
            //    {
            //        db.LineItems.Add(lineItem);
            //    }
            //}


            //if (jObject["upsell"] != null && jObject["upsell"]["upsellOriginalReceipt"] != null)
            //{
            //    List<JProperty> list = jObject.SelectToken("upsell").Children<JProperty>().ToList();

            //    Upsell upsell = new Upsell
            //    {
            //        id = Guid.NewGuid().ToString("N").Substring(0, 10),
            //        upsellOriginalReceipt = jObject["upsell"]["upsellOriginalReceipt"] != null ? (string)list.First(x => x.Name == "upsellOriginalReceipt").Value: "NA",
            //        upsellFlowId = jObject["upsell"]["upsellFlowId"] != null ? (int)list.First(x => x.Name == "upsellFlowId").Value: 0,
            //        upsellSession = jObject["upsell"]["upsellSession"] != null ? (string)list.First(x => x.Name == "upsellSession").Value: "NA",
            //        upsellPath = jObject["upsell"]["upsellPath"] != null ? (string)list.First(x => x.Name == "upsellPath").Value: "NA"
            //    };

            //    db.Upsells.Add(upsell);
            //}



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

        //public async Task<IHttpActionResult> Get()
        //{
        //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.clickbank.com/rest/1.3/orders2/AXN66NVG");
        //    request.Accept = "application/json";
        //    request.Headers.Add(HttpRequestHeader.Authorization, "DEV-SHE17V09PLJ3MARVSI502TNSEULV7U09:API-PHI76F2Q3KL52NIRBLKOHDQQFP2V5A3E");
        //    request.Method = "GET";

        //    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        //    Console.WriteLine(response.StatusCode);
        //    Console.WriteLine(response.StatusDescription);
        //    Stream resStream = response.GetResponseStream();
        //    StringBuilder sb = new StringBuilder();

        //    string tempString = null;
        //    int count = 0;
        //    byte[] buf = new byte[8192];

        //    do
        //    {
        //        count = resStream.Read(buf, 0, buf.Length);

        //        if (count != 0)
        //        {
        //            tempString = Encoding.ASCII.GetString(buf, 0, count);
        //            sb.Append(tempString);
        //        }
        //    }
        //    while (count > 0);

            

        //    return Ok(sb.ToString());
        //}
    }
}

public struct Notification
{
    public string notification;
    public string iv;
}
