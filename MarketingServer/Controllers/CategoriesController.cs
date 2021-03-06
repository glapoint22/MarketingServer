using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
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
        [AllowAnonymous]
        public async Task<IHttpActionResult> GetCategories()
        {
            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new {
                    id = x.ID,
                    name = x.Name,
                    featured = x.Featured,
                    icon = x.Icon,
                    niches = x.Niches
                        .OrderBy(z => z.Name)
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

        [Route("api/Categories/Manager")]
        public async Task<IHttpActionResult> GetManagerCategories()
        {
            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    id = x.ID,
                    name = x.Name,
                    featured = x.Featured,
                    icon = x.Icon,
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
                                        .Select(r => new
                                        {
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

            // Remove any unused images in the images directory
            FileManager.DeleteUnusedFiles(await FileManager.GetDBImages(), "~/Images/");

            return Ok(categories);
        }

        // PUT: api/Categories/5
        [ResponseType(typeof(void))]
        [HttpPut]
        public async Task<IHttpActionResult> PutCategory(Category[] categories)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (Category category in categories)
            {
                // Check to see if this category has been modified
                Category dbCategory = db.Categories.AsNoTracking().FirstOrDefault(x => x.ID == category.ID);

                if (dbCategory.Name != category.Name || dbCategory.Icon != category.Icon || dbCategory.Featured != category.Featured)
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
        public async Task<IHttpActionResult> DeleteCategory(string itemIds)
        {
            string[] ids = itemIds.Split(',');
            
            foreach(string id in ids)
            {

                // Get the current category
                Category category = await db.Categories.FindAsync(int.Parse(id));
                if (category == null)
                {
                    return NotFound();
                }

                // List of images to delete from the images directory
                List<string> imagesToDelete = new List<string>();

                // Add category images to the list
                if (category.Icon != null) imagesToDelete.Add(category.Icon);

                // Add niche and product images to the list
                category.Niches.ToList().ForEach(x =>
                {
                    if (x.Icon != null) imagesToDelete.Add(x.Icon);
                    x.Products.ToList().ForEach(y =>
                    {
                        if (y.Image != null) imagesToDelete.Add(y.Image);
                        y.ProductBanners.ToList().ForEach(z => imagesToDelete.Add(z.Name));
                    });
                });

                // Delete the images
                imagesToDelete.ForEach(x => ImageController.DeleteImageFile(x));

                // Remove this category from the database
                db.Categories.Remove(category);
            }

            await db.SaveChangesAsync();
            return Ok();
        }
    }
}