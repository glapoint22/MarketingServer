using System;
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
    public class CategoriesController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Categories
        public async Task<IHttpActionResult> GetCategories()
        {
            var categories = await db.Categories
                .Select(x => new {
                    id = x.ID,
                    name = x.Name,
                    featured = x.Featured,
                    icon = x.Icon,
                    categoryImage = x.CategoryImages
                        .Where(c => c.Selected)
                        .Select(c => new {
                            categoryId = c.CategoryID,
                            name = c.Name
                        })
                        .FirstOrDefault(),
                    niches = x.Niches
                        .Select(z => new {
                            id = z.ID,
                            name = z.Name
                        }).ToList()
                }
            )
            .ToListAsync();

            return Ok(categories);
        }



        public async Task<IHttpActionResult> GetCategories(bool includeProducts)
        {
            var categories = await db.Categories
                .Select(x => new
                {
                    id = x.ID,
                    name = x.Name,
                    featured = x.Featured,
                    icon = x.Icon,
                    categoryImage = x.CategoryImages
                        .Where(c => c.Selected)
                        .Select(c => new
                        {
                            categoryId = c.CategoryID,
                            name = c.Name
                        })
                        .FirstOrDefault(),
                    niches = x.Niches
                        .Select(z => new
                        {
                            id = z.ID,
                            name = z.Name,
                            products = z.Products
                                .Select(p => new
                                {
                                    id = p.ID,
                                    name = p.Name,
                                    hopLink = p.HopLink,
                                    description = p.Description,
                                    price = p.Price,
                                    featured = p.Featured,
                                    filters = db.ProductFilters
                                        .Where(q => q.ProductID == p.ID)
                                        .Select(q => q.FilterLabelID)
                                })
                                .ToList()
                        }).ToList()
                }
            )
            .ToListAsync();

            return Ok(categories);
        }








        // GET: api/Categories/5
        [ResponseType(typeof(Category))]
        public async Task<IHttpActionResult> GetCategory(int id)
        {
            Category category = await db.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        // PUT: api/Categories/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutCategory(int id, Category category)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != category.ID)
            {
                return BadRequest();
            }

            db.Entry(category).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
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

        // POST: api/Categories
        [ResponseType(typeof(Category))]
        public async Task<IHttpActionResult> PostCategory(Category category)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = category.ID }, category);
        }

        // DELETE: api/Categories/5
        [ResponseType(typeof(Category))]
        public async Task<IHttpActionResult> DeleteCategory(int id)
        {
            Category category = await db.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            db.Categories.Remove(category);
            await db.SaveChangesAsync();

            return Ok(category);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CategoryExists(int id)
        {
            return db.Categories.Count(e => e.ID == id) > 0;
        }
    }
}