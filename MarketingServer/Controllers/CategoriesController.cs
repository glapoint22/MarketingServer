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
                            name = z.Name,
                            icon = z.Icon
                        }).ToList()
                }
            )
            .ToListAsync();

            return Ok(categories);
        }



        public async Task<IHttpActionResult> GetCategories(bool includeProducts)
        {
            var categories = await db.Categories
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    id = x.ID,
                    name = x.Name,
                    featured = x.Featured,
                    icon = x.Icon,
                    categoryImages = x.CategoryImages
                        .Select(c => new
                        {
                            //categoryId = c.CategoryID,
                            name = c.Name,
                            isSelected = c.Selected
                        })
                        .ToList(),
                    niches = x.Niches
                        .Select(z => new
                        {
                            id = z.ID,
                            name = z.Name,
                            icon = z.Icon,
                            products = z.Products
                                .Select(p => new
                                {
                                    id = p.ID,
                                    name = p.Name,
                                    hopLink = p.HopLink,
                                    description = p.Description,
                                    image = p.Image,
                                    price = p.Price,
                                    featured = p.Featured,
                                    videos = p.ProductVideos
                                        .Where(s => s.ProductID == p.ID)
                                        .Select(s => s.Url)
                                        .ToList(),
                                    banners = p.ProductBanners
                                        .Where(r => r.ProductID == p.ID)
                                        .Select(r => new {
                                            name = r.Name,
                                            isSelected = r.Selected
                                        })
                                        .ToList(),
                                    filters = p.ProductFilters
                                        .Where(q => q.ProductID == p.ID)
                                        .Select(q => new {
                                            id = q.ID,
                                            filterOption = q.FilterLabelID
                                        })
                                        .ToList()
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
        public async Task<IHttpActionResult> PutCategory(Category[] categories)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (Category category in categories)
            {
                // Get a list of category images for this category that is stored in the database
                List<CategoryImage> dbCategoryImages = await db.CategoryImages.ToListAsync();
                
                // Check to see if any category images have been deleted
                foreach (CategoryImage dbCategoryImage in dbCategoryImages)
                {
                    if(!category.CategoryImages.Select(x => x.Name).ToList().Contains(dbCategoryImage.Name))
                    {
                        db.CategoryImages.Remove(dbCategoryImage);
                    }
                }

                // Check to see if any category images need to be added or have been modified
                foreach (CategoryImage categoryImage in category.CategoryImages)
                {
                    if (!(db.CategoryImages.Count(x => x.Name == categoryImage.Name) > 0))
                    {
                        db.Entry(categoryImage).State = EntityState.Added;
                    }
                    else
                    {
                        CategoryImage dbCategoryImage = db.CategoryImages.FirstOrDefault(x => x.Name == categoryImage.Name);
                        db.Entry(dbCategoryImage).State = EntityState.Detached;
                        if (dbCategoryImage.Selected != categoryImage.Selected)
                        {
                            db.Entry(categoryImage).State = EntityState.Modified;
                        }
                    }
                }

                // Check to see if this category has been modified
                Category dbCategory = db.Categories.FirstOrDefault(x => x.ID == category.ID);
                db.Entry(dbCategory).State = EntityState.Detached;

                if (dbCategory.Name != category.Name || dbCategory.Icon != category.Icon)
                {
                    db.Entry(category).State = EntityState.Modified;
                }
            }

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }

            return Ok();
        }

        // POST: api/Categories
        [ResponseType(typeof(Category))]
        public async Task<IHttpActionResult> PostCategory(Category[] categories)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach(Category category in categories)
            {
                db.Categories.Add(category);
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Categories/5
        [ResponseType(typeof(Category))]
        public async Task<IHttpActionResult> DeleteCategory(int[] ids)
        {
            foreach(int id in ids)
            {
                Category category = await db.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                db.Categories.Remove(category);
            }

            await db.SaveChangesAsync();
            return Ok();
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