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
    
    public partial class Subscription
    {
        public System.Guid CustomerID { get; set; }
        public int NicheID { get; set; }
        public System.Guid NextEmailToSend { get; set; }
        public bool Active { get; set; }
        public bool Subscribed { get; set; }
        public System.DateTime DateSubscribed { get; set; }
        public Nullable<System.DateTime> DateUnsubscribed { get; set; }
    
        public virtual Customer Customer { get; set; }
        public virtual Email Email { get; set; }
        public virtual Nich Nich { get; set; }
    }
}