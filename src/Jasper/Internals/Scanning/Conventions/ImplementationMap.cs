﻿using System.Linq;
using Jasper.Internals.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Jasper.Internals.Scanning.Conventions
{
    public class ImplementationMap : IRegistrationConvention
    {
        public void ScanTypes(TypeSet types, IServiceCollection services)
        {
            var interfaces = types.FindTypes(TypeClassification.Interfaces);
            var concretes = types.FindTypes(TypeClassification.Concretes).Where(x => TypeExtensions.HasConstructors(x)).ToArray();

            interfaces.Each(@interface =>
            {
                var implementors = concretes.Where(x => x.CanBeCastTo(@interface)).ToArray();
                if (implementors.Count() == 1)
                {
                    services.AddType(@interface, implementors.Single());
                }
            });
        }

        public override string ToString()
        {
            return "Register any single implementation of any interface against that interface";
        }
    }
}