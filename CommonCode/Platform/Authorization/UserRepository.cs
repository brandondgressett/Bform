using BFormDomain.CommonCode.Authorization;
using BFormDomain.Diagnostics;
using BFormDomain.CommonCode.Repository.Mongo;
using BFormDomain.Repository;
using BFormDomain.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Platform.Authorization;
using BFormDomain.CommonCode.Platform.Tenancy;
using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Platform.Authorization
{
    public class UserRepository : TenantAwareMongoRepository<ApplicationUser>
    {

        public UserRepository(
            ITenantContext tenantContext,
            ITenantConnectionProvider connectionProvider,
            IOptions<MultiTenancyOptions> multiTenancyOptions,
            SimpleApplicationAlert alerts,
            ILogger<UserRepository>? logger = null) 
            : base(tenantContext, connectionProvider, multiTenancyOptions, alerts, logger)
        {

        }

        protected override string CollectionName => nameof(ApplicationUser);

        protected override IMongoCollection<ApplicationUser> CreateCollection()
        {
            var collection = OpenCollection();
            
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.Roles));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.UserName));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.AccessFailedCount));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.Claims));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.ConcurrencyStamp));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.CreatedOn));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.Email));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.EmailConfirmed));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.LockoutEnabled));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.LockoutEnd));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.Logins));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.NormalizedEmail));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.NormalizedUserName));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.PasswordHash));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.PhoneNumber));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.PhoneNumberConfirmed));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.SecurityStamp));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.Tags));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.TimeZoneId));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.Tokens));
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.TwoFactorEnabled));
            
            // Multi-tenancy indexes
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.TenantId)); // For validation only
            collection.AssureIndex(Builders<ApplicationUser>.IndexKeys.Ascending(it => it.IsSuperAdmin));

            return collection;
        }
        
        #region Cross-Tenant Operations (Super Admin Only)
        
        /// <summary>
        /// Gets users across multiple tenants. Only available to super admins.
        /// </summary>
        public async Task<List<ApplicationUser>> GetUsersAcrossTenantsAsync(List<Guid> tenantIds)
        {
            if (!_tenantContext.IsRootUser)
            {
                throw new UnauthorizedAccessException("Only super admins can query across tenants");
            }
            
            return await QueryAcrossTenantsAsync(tenantIds);
        }
        
        /// <summary>
        /// Gets all super admin users across all tenants.
        /// </summary>
        public async Task<List<ApplicationUser>> GetAllSuperAdminsAsync()
        {
            if (!_tenantContext.IsRootUser)
            {
                throw new UnauthorizedAccessException("Only super admins can query for super admins");
            }
            
            // Get all tenants from tenant repository
            // For now, we'll query the current tenant's super admins
            var filter = Builders<ApplicationUser>.Filter.Eq(u => u.IsSuperAdmin, true);
            var collection = GuardedCreateCollection();
            return await collection.Find(filter).ToListAsync();
        }
        
        /// <summary>
        /// Finds a user by username. Since each tenant has their own database,
        /// this will only search within the current tenant's database.
        /// </summary>
        public async Task<ApplicationUser?> FindByUsernameAsync(string username)
        {
            var filter = Builders<ApplicationUser>.Filter.Eq(u => u.UserName, username);
            var collection = GuardedCreateCollection();
            return await collection.Find(filter).FirstOrDefaultAsync();
        }
        
        #endregion
    }
}
