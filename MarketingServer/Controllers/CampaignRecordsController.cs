﻿using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace MarketingServer.Controllers
{
    public class CampaignRecordsController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/CampaignRecords/5
        [ResponseType(typeof(CampaignRecord))]
        public async Task<IHttpActionResult> GetCampaignRecord(string subscriptionID)
        {
            var campaignRecord = await db.CampaignRecords
                .AsNoTracking()
                .OrderByDescending(x => x.Date)
                .Where(x => x.SubscriptionID == subscriptionID && !x.ProductPurchased && !x.Ended)
                .Select(x => new
                {
                    SubscriptionID = x.SubscriptionID,
                    Date = x.Date,
                    ProductID = x.ProductID,
                    Day = x.Day,
                    ProductPurchased = x.ProductPurchased,
                    Ended = x.Ended
                })
                .FirstOrDefaultAsync();

            if (campaignRecord == null)
            {
                return NotFound();
            }

            return Ok(campaignRecord);
        }

        // PUT: api/CampaignRecords/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutCampaignRecord(CampaignRecord campaignRecord)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Entry(campaignRecord).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(HttpStatusCode.InternalServerError);
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/CampaignRecords
        [ResponseType(typeof(CampaignRecord))]
        public async Task<IHttpActionResult> PostCampaignRecord(CampaignRecord campaignRecord)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.CampaignRecords.Add(campaignRecord);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (CampaignRecordExists(campaignRecord.SubscriptionID))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = campaignRecord.SubscriptionID }, campaignRecord);
        }


        private bool CampaignRecordExists(string id)
        {
            return db.CampaignRecords.Count(e => e.SubscriptionID == id) > 0;
        }
    }
}