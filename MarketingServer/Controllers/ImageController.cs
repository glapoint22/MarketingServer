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
            postedFile.SaveAs(filePath);


            return Request.CreateErrorResponse(HttpStatusCode.Created, "Image Uploaded Successfully.");
        }
    }
}
