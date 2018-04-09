using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    }
}
