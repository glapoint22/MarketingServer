//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MarketingServer
{
    using System;
    using System.Collections.Generic;
    
    public partial class EmailCampaign
    {
        public string ID { get; set; }
        public string ProductID { get; set; }
        public Nullable<int> Day { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    
        public virtual Product Product { get; set; }
    }
}