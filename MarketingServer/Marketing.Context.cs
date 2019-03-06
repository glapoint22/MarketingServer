﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class MarketingEntities : DbContext
    {
        public MarketingEntities()
            : base("name=MarketingEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<CampaignRecord> CampaignRecords { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Client> Clients { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<EmailCampaign> EmailCampaigns { get; set; }
        public virtual DbSet<FilterLabel> FilterLabels { get; set; }
        public virtual DbSet<Filter> Filters { get; set; }
        public virtual DbSet<LeadMagnetEmail> LeadMagnetEmails { get; set; }
        public virtual DbSet<LeadPage> LeadPages { get; set; }
        public virtual DbSet<Nich> Niches { get; set; }
        public virtual DbSet<ProductBanner> ProductBanners { get; set; }
        public virtual DbSet<ProductFilter> ProductFilters { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductVideo> ProductVideos { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<Subscription> Subscriptions { get; set; }
        public virtual DbSet<PriceRange> PriceRanges { get; set; }
    }
}
