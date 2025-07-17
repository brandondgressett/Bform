using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BFormDomain.CommonCode.Authorization;
using BFormDomain.Repository;
using BFormDomain.DataModels;

namespace BFormDomain.CommonCode.Platform.Users
{
    public class ApplicationUserLogic
    {
        private readonly IRepositoryFactory _repositoryFactory;
        
        public ApplicationUserLogic(IRepositoryFactory repositoryFactory)
        {
            _repositoryFactory = repositoryFactory;
        }

        public async Task<ApplicationUser?> LoadFromUserIdStringAsync(string? userId, CancellationToken cancellationToken = default)
        {
            if (userId == null) return null;
            var repository = _repositoryFactory.CreateRepository<ApplicationUser>();
            var (user, _) = await repository.LoadAsync(Guid.Parse(userId));
            return user;
        }
        
        public async Task<ApplicationUser?> LoadFromUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var repository = _repositoryFactory.CreateRepository<ApplicationUser>();
            var (user, _) = await repository.LoadAsync(userId);
            return user;
        }
        
        public async Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var repository = _repositoryFactory.CreateRepository<ApplicationUser>();
            var (users, _) = await repository.GetAsync(predicate: u => u.UserName == username);
            return users.FirstOrDefault();
        }
        
        public async Task<string[]> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Stub implementation - would normally load from roles/permissions
            return await Task.FromResult(Array.Empty<string>());
        }
    }
}
