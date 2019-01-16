using System.Web;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Configuration;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace MarketingServer
{
    public class Mail
    {
        private SmtpClient smtpClient = new SmtpClient();
        public string from;
        public string to;
        public string subject;
        public string body;
        
        public Mail(string emailId, Customer customer, string subject, string body, List<Product> products)
        {
            // Get the mail settings from web.config
            Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration(HttpRuntime.AppDomainAppVirtualPath);
            MailSettingsSectionGroup mailSettings = configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;

            // Get the domain name
            //string domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority;
            string domain = "http://www.nicheshack.com";

            // Remove summary from the body
            body = Regex.Replace(body, @"summary=""[a-zA-Z0-9-.]+""", "");

            // Remove title from the body
            body = Regex.Replace(body, @"title=""[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)""", "");

            // Replace localhost with the domain name
            body = Regex.Replace(body, @"http://localhost(?::[0-9]+)?", domain);

            // Get the email footer
            string filePath = HttpContext.Current.Server.MapPath("~/EmailFooter.txt");
            StreamReader file = new StreamReader(filePath);

            string emailEndPattern = @"<\/table><!--\[if \(gte mso 9\)\|\(IE\)\]><\/td><\/tr><\/table><!\[endif\]--><\/td><\/tr><\/table>$";



            body = Regex.Replace(body, emailEndPattern, AddProducts(products));


            // Get the footer from the file
            string footer = "";
            string line;

            while((line = file.ReadLine()) != null)
            {
                footer += line;
            }

            file.Close();


            // Add the footer to the body
            body = Regex.Replace(body, emailEndPattern, footer);

            // Set the email properties
            this.subject = subject;
            this.body = string.Format(body, customer.Name, emailId, customer.ID, domain);
            to = customer.Email;
            from = mailSettings.Smtp.Network.UserName;
        }

        public async Task Send()
        {
            MailMessage mailMessage = new MailMessage(from, to, subject, body);
            mailMessage.IsBodyHtml = true;
            await smtpClient.SendMailAsync(mailMessage);
        }

        public string AddProducts(List<Product> products)
        {
            // Get the email footer
            string filePath = HttpContext.Current.Server.MapPath("~/Products.txt");
            StreamReader file = new StreamReader(filePath);

            string line;
            string caption = string.Empty;
            string productsRow = string.Empty;
            //string productContainer = string.Empty;
            string singleProduct = string.Empty;
            string multipleProducts = string.Empty;
            string product = string.Empty;
            //string imageContainer = string.Empty;
            //string imageContainerEnd = string.Empty;
            string productEnd = string.Empty;
            string productsRowEnd = string.Empty;
            string documentEnd = string.Empty;
            string productsText = string.Empty;

            while ((line = file.ReadLine()) != "<!--Product Row-->")
            {
                caption += line.Trim();
            }

            while ((line = file.ReadLine()).Trim() != "<!--Single Product-->")
            {
                productsRow += line.Trim();
            }

            while ((line = file.ReadLine()).Trim() != "<!--Multiple Products-->")
            {
                singleProduct += line.Trim();
            }

            while ((line = file.ReadLine()).Trim() != "<!--Product-->")
            {
                multipleProducts += line.Trim();
            }

            while ((line = file.ReadLine()).Trim() != "<!--Product End-->")
            {
                product += line.Trim();
            }

            //while ((line = file.ReadLine()).Trim() != "<!--Image Container End-->")
            //{
            //    imageContainer += line.Trim();
            //}

            //while ((line = file.ReadLine()).Trim() != "<!--Product End-->")
            //{
            //    imageContainerEnd += line.Trim();
            //}

            while ((line = file.ReadLine()).Trim() != "<!--Product Row End-->")
            {
                productEnd += line.Trim();
            }

            while ((line = file.ReadLine()).Trim() != "<!--Document End-->")
            {
                productsRowEnd += line.Trim();
            }

            while ((line = file.ReadLine()) != null)
            {
                documentEnd += line.Trim();
            }

            file.Close();

            

            for(int i = 0; i < products.Count; i++)
            {
                if(i % 2 == 0)
                {
                    productsText += productsRow + (i == products.Count - 1 ? singleProduct : multipleProducts);
                }else
                {
                    productsText += multipleProducts;
                }



                productsText += product;

                productsText += productEnd +  (i != products.Count - 1 ? "</div>" : "");

                if (i % 2 == 1)
                {
                    productsText += productsRowEnd;
                }

            }

            return caption + productsText + documentEnd;
        }
    }
}