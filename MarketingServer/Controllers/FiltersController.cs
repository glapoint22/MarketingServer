﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using MarketingServer;

namespace MarketingServer.Controllers
{
    public class FiltersController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Filters
        public async Task<IHttpActionResult> GetFilters()
        {
            var filters = await db.Filters.Select(x => new {
                id = x.ID,
                name = x.Name,
                options = x.FilterLabels
                    .Where(y => y.FilterID == x.ID)
                    .Select(y => new {
                        id = y.ID,
                        name = y.Name
                    })
                    .ToList()
            })
            .ToListAsync();

            return Ok(filters);
        }

        // GET: api/Filters/5
        [ResponseType(typeof(Filter))]
        public async Task<IHttpActionResult> GetFilter(int id)
        {
            Filter filter = await db.Filters.FindAsync(id);
            if (filter == null)
            {
                return NotFound();
            }

            return Ok(filter);
        }

        // PUT: api/Filters/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutFilter(int id, Filter filter)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != filter.ID)
            {
                return BadRequest();
            }

            db.Entry(filter).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FilterExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Filters
        [ResponseType(typeof(Filter))]
        public async Task<IHttpActionResult> PostFilter(Filter filter)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Filters.Add(filter);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = filter.ID }, filter);
        }

        // DELETE: api/Filters/5
        [ResponseType(typeof(Filter))]
        public async Task<IHttpActionResult> DeleteFilter(int id)
        {
            Filter filter = await db.Filters.FindAsync(id);
            if (filter == null)
            {
                return NotFound();
            }

            db.Filters.Remove(filter);
            await db.SaveChangesAsync();

            return Ok(filter);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool FilterExists(int id)
        {
            return db.Filters.Count(e => e.ID == id) > 0;
        }
    }
}