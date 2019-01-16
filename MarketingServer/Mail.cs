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


            // Get the end of the email
            string emailEndPattern = @"<\/table><!--\[if \(gte mso 9\)\|\(IE\)\]><\/td><\/tr><\/table><!\[endif\]--><\/td><\/tr><\/table>$";

            // Add products
            if(products.Count > 0) body = Regex.Replace(body, emailEndPattern, AddProducts(products, domain));

            // Add the footer
            body = Regex.Replace(body, emailEndPattern, AddFooter());

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

        public string AddFooter()
        {
            // Get the email footer
            string filePath = HttpContext.Current.Server.MapPath("~/EmailFooter.txt");
            StreamReader file = new StreamReader(filePath);

            // Read the text from the file
            string footer = "";
            string line;

            while ((line = file.ReadLine()) != null)
            {
                footer += line.Trim();
            }

            file.Close();

            return footer;
        }

        public string AddProducts(List<Product> products, string domain)
        {
            // Read the products text
            string filePath = HttpContext.Current.Server.MapPath("~/Products.txt");
            StreamReader file = new StreamReader(filePath);

            string line;
            string caption = string.Empty;
            string productsRow = string.Empty;
            string singleProduct = string.Empty;
            string multipleProducts = string.Empty;
            string product = string.Empty;
            string productEnd = string.Empty;
            string productsRowEnd = string.Empty;
            string documentEnd = string.Empty;
            string productsText = string.Empty;
            string outlookProductStart = string.Empty;
            string outlookProductContinue = string.Empty;
            string outlookProductEnd = string.Empty;


            // Read the sections of the file
            while ((line = file.ReadLine()) != "<!--Outlook Product Continue-->")
            {
                outlookProductStart += line.Trim();
            }

            while ((line = file.ReadLine()) != "<!--Outlook Product End-->")
            {
                outlookProductContinue += line.Trim();
            }

            while ((line = file.ReadLine()) != "<!--Caption-->")
            {
                outlookProductEnd += line.Trim();
            }

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


            // Loop through all the products
            for (int i = 0; i < products.Count; i++)
            {   
                // First product in the row
                if (i % 2 == 0)
                {
                    productsText += productsRow + (i == products.Count - 1 ? singleProduct : outlookProductStart + multipleProducts);
                }
                // Last product in the row
                else
                {
                    productsText += outlookProductContinue + multipleProducts;
                }


                // Get product info
                productsText += string.Format(product, products[i].HopLink, products[i].Image, domain);

                // End of product
                productsText += productEnd + (i != products.Count - 1 ? "</div>" : "");

                // End of row
                if (i % 2 == 1 || i == products.Count - 1)
                {
                    productsText += outlookProductEnd + productsRowEnd;
                }
            }

            return caption + productsText + documentEnd;
        }
    }
}