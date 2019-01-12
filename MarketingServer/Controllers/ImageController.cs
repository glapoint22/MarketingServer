using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace MarketingServer
{
    public class ImageController : ApiController
    {
        [HttpPost]
        public HttpResponseMessage PostImage()
        {
            HttpPostedFile postedFile = HttpContext.Current.Request.Files["image"];
            string filePath = HttpContext.Current.Server.MapPath("~/Images/" + postedFile.FileName);

            int imageIndex = filePath.LastIndexOf("\\") + 1;
            string imageName = filePath.Substring(imageIndex);
            int imageExt = imageName.IndexOf(".");

            string newImageName = Guid.NewGuid().ToString("N") + imageName.Substring(imageExt);

            filePath = filePath.Substring(0, imageIndex) + newImageName;


            postedFile.SaveAs(filePath);


            return Request.CreateResponse(HttpStatusCode.OK, newImageName);
        }

        public static void DeleteImageFile(string image)
        {
            string filePath = HttpContext.Current.Server.MapPath("~/Images/" + image);
            File.Delete(filePath);
        }

        public void DeleteImage(string itemIds)
        {
            DeleteImageFile(itemIds);
        }

        public static async Task<List<string>> GetDBImages()
        {
            MarketingEntities db = new MarketingEntities();
            List<string> images = new List<string>();

            // Get a list of images in the database
            images.AddRange(await db.Categories.Where(x => x.Icon != null).Select(x => x.Icon).ToListAsync());
            images.AddRange(await db.Niches.Where(x => x.Icon != null).Select(x => x.Icon).ToListAsync());
            images.AddRange(await db.Products.Where(x => x.Image != null).Select(x => x.Image).ToListAsync());
            images.AddRange(await db.CategoryImages.Select(x => x.Name).ToListAsync());
            images.AddRange(await db.ProductBanners.Select(x => x.Name).ToListAsync());

            // Get the images from emails and leads
            List<string> bodies = await db.LeadMagnetEmails.Select(x => x.Body).ToListAsync();
            bodies.AddRange(await db.EmailCampaigns.Select(x => x.Body).ToListAsync());
            bodies.AddRange(await db.LeadPages.Select(x => x.Body).ToListAsync());
            foreach (string body in bodies)
            {
                MatchCollection matchList = Regex.Matches(body, @"(?:\/Images\/)([a-z0-9]+\.(jpg|jpeg|gif|png|bmp|tiff|tga|svg))");
                if (matchList.Count > 0)
                {
                    images.AddRange(matchList.Cast<Match>().Select(match => match.Groups[1].Value).ToList());
                }
            }

            return images;
        }
    }
}