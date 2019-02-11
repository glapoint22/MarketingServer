using System;
using System.Web.Http;

namespace MarketingServer.Controllers
{
    public class TimeController : ApiController
    {
        [AllowAnonymous]
        public IHttpActionResult GetTime()
        {
            var time = new
            {
                local = DateTime.Now,
                utc = DateTime.UtcNow
            };

            return Ok(time);
        }
    }
}
