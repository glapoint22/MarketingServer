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
    
    public partial class Customer
    {
        public string Email { get; set; }
        public int NicheID { get; set; }
        public string Name { get; set; }
        public int CampaignID { get; set; }
        public int CurrentCampaignDay { get; set; }
        public System.DateTime EmailSendDate { get; set; }
    
        public virtual Campaign Campaign { get; set; }
        public virtual Nich Nich { get; set; }
    }
}
