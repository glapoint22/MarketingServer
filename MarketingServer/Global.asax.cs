using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Data;
using System.Data.Entity;

namespace MarketingServer
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private MarketingEntities db = new MarketingEntities();

        protected async void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            await Start();
        }

        public async Task Start()
        {
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.Credentials = new System.Net.NetworkCredential()
            {
                UserName = "glapoint22@gmail.com",
                Password = "Cyb668622"
            };
            smtpClient.EnableSsl = true;



            
            List<Customer> customers = await (from c in db.Customers
                                //where c.EmailSendDate == DateTime.Today
                                              select c).ToListAsync();


            foreach (Customer customer in customers)
            {
                var email = await db.Emails.Where(e => e.CampaignID == customer.CampaignID && e.Day == customer.CurrentCampaignDay).Select(e => new
                {
                    subject = e.Subject,
                    body = e.Body
                }).AsNoTracking().SingleAsync();

                MailMessage mailMessage = new MailMessage("glapoint22@gmail.com", customer.Email);
                mailMessage.Subject = email.subject;
                mailMessage.Body = email.body;
                //smtpClient.Send(mailMessage);



                int nextDay = await db.Emails.Where(e => e.Day > customer.CurrentCampaignDay && e.CampaignID == customer.CampaignID)
                            .Select(e => e.Day).FirstOrDefaultAsync();

                if(nextDay != 0)
                {
                    customer.CurrentCampaignDay = nextDay;
                }else
                {
                    int nextCampaignId = await db.Campaigns.Where(c => c.NicheID == customer.NicheID && c.ID > customer.CampaignID).Select(c => c.ID).FirstOrDefaultAsync();

                    if (nextCampaignId == 0) continue;

                    customer.CurrentCampaignDay = 1;
                    customer.CampaignID = nextCampaignId;
                }
                



                db.SaveChanges();
            }

            

           


            //while (true)
            //{
            //    
            //    await Task.Delay(30000);

            //}
        }
    }
}
