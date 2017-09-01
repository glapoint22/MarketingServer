using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Data;
using System.Data.Entity;

namespace MarketingServer
{
    public class WebApiApplication : HttpApplication
    {
        private MarketingEntities db = new MarketingEntities();

        protected async void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            await Run();
        }

        public async Task Run()
        {

            //TimeSpan span = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 28, 0) - DateTime.Now;

            //DateTime dateTime = DateTime.Parse("8:23 PM");
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.Credentials = new System.Net.NetworkCredential()
            {
                UserName = "glapoint22@gmail.com",
                Password = "Cyb668622"
            };
            smtpClient.EnableSsl = true;


            while (true)
            {
                await Task.Delay(5000);

                //Get a list of customers that we will be sending emails to
                List<Customer> customers = await db.Customers
                    .Where(c => c.CustomerCampaigns.Where(x => x.Active && x.Subscribed).Count() != 0
                    //&& c.EmailSendDate == DateTime.Today
                    )
                    .Select(c => c).ToListAsync();

                //Iterate through each customer
                foreach (Customer customer in customers)
                {
                    //Get a list of campaigns this customer is subscribed to
                    List<CustomerCampaign> campaigns = customer.CustomerCampaigns
                        .Where(c => c.Active && c.Subscribed)
                        .Select(c => c)
                        .ToList();

                    //Iterate through each campaign
                    foreach (CustomerCampaign campaign in campaigns)
                    {
                        //Compose the email and send
                        var email = await db.Emails.Where(e => e.CampaignID == campaign.CurrentCampaignID && e.Day == campaign.CurrentCampaignDay).Select(e => new
                        {
                            subject = e.Subject,
                            body = e.Body
                        }).AsNoTracking().SingleAsync();

                        MailMessage mailMessage = new MailMessage("glapoint22@gmail.com", customer.Email, email.subject, email.body);
                        //smtpClient.Send(mailMessage);


                        //Get the next email day
                        int nextDay = await db.Emails.Where(e => e.Day > campaign.CurrentCampaignDay && e.CampaignID == campaign.CurrentCampaignID)
                                    .Select(e => e.Day).FirstOrDefaultAsync();


                        //If there is a next email for this campaign
                        if (nextDay != 0)
                        {
                            campaign.CurrentCampaignDay = nextDay;
                        }
                        else
                        {
                            //There were no more emails for the current campaign, so lets grab another campaign id
                            int nextCampaignId = await db.Campaigns.Where(c => c.NicheID == campaign.NicheID && c.ID > campaign.CurrentCampaignID).Select(c => c.ID).FirstOrDefaultAsync();

                            //If there are no more campaigns left to this niche, set inactive
                            if (nextCampaignId == 0)
                            {
                                campaign.Active = false;
                                continue;
                            }

                            //Set the next campaign id and day
                            campaign.CurrentCampaignDay = 1;
                            campaign.CurrentCampaignID = nextCampaignId;
                        }

                        //Set when this customer will be receiving their next email
                        customer.EmailSendDate = customer.EmailSendDate.AddDays(customer.EmailFrequency);
                    }

                }

                //Update the database
                if (db.ChangeTracker.HasChanges())
                {
                    await db.SaveChangesAsync();
                }
            }
            
        }
    }
}
