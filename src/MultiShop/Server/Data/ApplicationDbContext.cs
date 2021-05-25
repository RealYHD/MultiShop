using MultiShop.Server.Models;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MultiShop.Shared.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MultiShop.Server.Data
{
    public class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options, IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<ResultsProfile>()
                .Property(e => e.Order)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, null), 
                    v => JsonSerializer.Deserialize<List<ResultsProfile.Category>>(v, null), 
                    new ValueComparer<IList<ResultsProfile.Category>>(
                        (a, b) => a.SequenceEqual(b),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => (IList<ResultsProfile.Category>) c.ToList()
                    )
                );

            modelBuilder.Entity<SearchProfile>()
                .Property(e => e.ShopStates)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, null), 
                    v => JsonSerializer.Deserialize<SearchProfile.ShopToggler>(v, null),
                    new ValueComparer<SearchProfile.ShopToggler>(
                        (a, b) => a.Equals(b),
                        c => c.GetHashCode(),
                        c => c.Clone()
                    )
                );
        }
    }
}
