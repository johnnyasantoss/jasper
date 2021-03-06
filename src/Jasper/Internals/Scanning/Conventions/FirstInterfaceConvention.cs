using System.Linq;
using Jasper.Internals.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Jasper.Internals.Scanning.Conventions
{
    public class FirstInterfaceConvention : IRegistrationConvention
    {
        public void ScanTypes(TypeSet types, IServiceCollection services)
        {
            foreach (var type in types.FindTypes(TypeClassification.Concretes).Where(x => x.HasConstructors()))
            {
                var interfaceType = type.AllInterfaces().FirstOrDefault();
                if (interfaceType != null)
                {
                    services.AddType(interfaceType, type);
                }
            }

        }

        public override string ToString()
        {
            return "Register all concrete types against the first interface (if any) that they implement";
        }
    }
}
