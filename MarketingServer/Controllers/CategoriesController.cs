using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using MarketingServer;
using System.Web;
using System.IO;

namespace MarketingServer.Controllers
{
    public class CategoriesController : ApiController
    {
        private MarketingEntities db = new MarketingEntities();

        // GET: api/Categories
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

        public async Task<IHttpActionResult> GetCategories(bool isManager)
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
                    categoryImages = x.CategoryImages
                        .Select(c => new
                        {
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
            DeleteUnusedFiles(await ImageController.GetDBImages(), "~/Images/");


            // Remove any unused files in the downloads directory
            DeleteUnusedFiles(await db.LeadPages.Select(x => x.LeadMagnet).ToListAsync(), "~/Downloads/");


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
                List<CategoryImage> dbCategoryImages = await db.CategoryImages.AsNoTracking().Where(x => x.CategoryID == category.ID).ToListAsync();
                
                // Check to see if any category images have been deleted
                foreach (CategoryImage dbCategoryImage in dbCategoryImages)
                {
                    if(!category.CategoryImages.Select(x => x.Name).ToList().Contains(dbCategoryImage.Name))
                    {
                        db.CategoryImages.Attach(dbCategoryImage);
                        db.CategoryImages.Remove(dbCategoryImage);
                        ImageController.DeleteImageFile(dbCategoryImage.Name);
                    }
                }

                // Check to see if any category images need to be added or have been modified
                foreach (CategoryImage categoryImage in category.CategoryImages)
                {
                    if (!(dbCategoryImages.Count(x => x.Name == categoryImage.Name) > 0))
                    {
                        db.Entry(categoryImage).State = EntityState.Added;
                    }
                    else
                    {
                        CategoryImage dbCategoryImage = dbCategoryImages.FirstOrDefault(x => x.Name == categoryImage.Name);
                        if (dbCategoryImage.Selected != categoryImage.Selected)
                        {
                            db.Entry(categoryImage).State = EntityState.Modified;
                        }
                    }
                }

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
                category.CategoryImages.ToList().ForEach(x => imagesToDelete.Add(x.Name));

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

        public static void DeleteUnusedFiles(List<string> dbFiles, string directory)
        {
            // Get all files in the directory
            string dir = HttpContext.Current.Server.MapPath(directory);
            string[] files = Directory.GetFiles(dir);

            // Delete any file that is not in the database
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                if (dbFiles.FindIndex(x => x == fileName) == -1)
                {
                    File.Delete(file);
                }
            }
        }
    }
}