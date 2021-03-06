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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Subscription()
        {
            this.CampaignRecords = new HashSet<CampaignRecord>();
        }
    
        public string ID { get; set; }
        public string CustomerID { get; set; }
        public int NicheID { get; set; }
        public bool Subscribed { get; set; }
        public bool Suspended { get; set; }
        public System.DateTime DateSubscribed { get; set; }
        public Nullable<System.DateTime> DateUnsubscribed { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CampaignRecord> CampaignRecords { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual Nich Nich { get; set; }
    }
}
