using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace MarketingServer
{
    public class FileManager
    {
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

        public static async Task<List<string>> GetDBImages()
        {
            MarketingEntities db = new MarketingEntities();
            List<string> images = new List<string>();

            // Get a list of images in the database
            images.AddRange(await db.Categories.AsNoTracking().Where(x => x.Icon != null).Select(x => x.Icon).ToListAsync());
            images.AddRange(await db.Niches.AsNoTracking().Where(x => x.Icon != null).Select(x => x.Icon).ToListAsync());
            images.AddRange(await db.Products.AsNoTracking().Where(x => x.Image != null).Select(x => x.Image).ToListAsync());
            images.AddRange(await db.CategoryImages.AsNoTracking().Select(x => x.Name).ToListAsync());
            images.AddRange(await db.ProductBanners.AsNoTracking().Select(x => x.Name).ToListAsync());

            // Get the images from emails and leads
            List<string> bodies = await db.LeadMagnetEmails.AsNoTracking().Select(x => x.Body).ToListAsync();
            bodies.AddRange(await db.EmailCampaigns.AsNoTracking().Select(x => x.Body).ToListAsync());
            bodies.AddRange(await db.LeadPages.AsNoTracking().Select(x => x.Body).ToListAsync());
            foreach (string body in bodies)
            {
                MatchCollection matchList = Regex.Matches(body, @"(?:\/Images\/)([a-z0-9_]+\.(jpg|jpeg|gif|png|bmp|tiff|tga|svg))");
                if (matchList.Count > 0)
                {
                    images.AddRange(matchList.Cast<Match>().Select(match => match.Groups[1].Value).ToList());
                }
            }

            return images;
        }
    }
}