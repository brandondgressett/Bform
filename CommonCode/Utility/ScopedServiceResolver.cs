using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Utility
{
    public delegate object ScopedServiceResolver(object serviceType);

    public static class KeyInjectExtensions
    {

        public static Func<IServiceProvider, ScopedServiceResolver> Factory(params (object, Type)[] mapping)
        {
            return provider => (ScopedServiceResolver)((object serviceType) =>
            {
                using var scope = provider.CreateScope();
                var match = mapping.First(x => x.Item1.Equals(serviceType));
                var svc = scope.ServiceProvider.GetRequiredService(match.Item2);
                return svc;
            });
        }
    }

}
