using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MarketingServer
{
    public class CampaignEmail
    {
        public string productId;
        public int day;
        public Customer customer;

        public CampaignEmail(string productId, int day, Customer customer)
        {
            this.productId = productId;
            this.day = day;
            this.customer = customer;
        }
    }
}