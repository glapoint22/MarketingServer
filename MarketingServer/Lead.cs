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
    
    public partial class Lead
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Lead()
        {
            this.Niches = new HashSet<Nich>();
        }
    
        public int ID { get; set; }
        public string LeadPage { get; set; }
        public string LeadMagnet { get; set; }
        public string MainStyle { get; set; }
        public string Image { get; set; }
        public string Text { get; set; }
        public string TextStyle { get; set; }
        public string BarStyle { get; set; }
        public string BarText { get; set; }
        public string ButtonStyle { get; set; }
        public string ButtonText { get; set; }
        public string FormButtonText { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Nich> Niches { get; set; }
    }
}