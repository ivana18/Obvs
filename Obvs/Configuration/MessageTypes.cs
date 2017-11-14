﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Obvs.Extensions;

namespace Obvs.Configuration
{
    public static class MessageTypes
    {
        /// <summary>
        /// This is inspired by http://www.michael-whelan.net/replacing-appdomain-in-dotnet-core/
        /// 
        /// More specifically:
        /// https://github.com/mwhelan/Specify/blob/master/src/app/Specify/lib/AssemblyTypeResolver.cs
        /// </summary>
        static class AppDomainReplacement
        {
            // set assembly name to nameof Obvs namespace - likely to match.
            private const string ObvsAssemblyName = nameof(Obvs);

            private static string GetRootAssemblyName()
            {
                return ObvsAssemblyName;
            }
            
            /// <summary>
            /// Gets all assemblies from the AppDomain.
            /// </summary>
            /// <returns>IEnumerable&lt;Assembly&gt;.</returns>
            public static IEnumerable<Assembly> GetAllAssembliesFromAppDomain()
            {
                var rootAssemblyName = GetRootAssemblyName();
                var assemblies = new List<Assembly>();
                var dependencies = DependencyContext.Default.RuntimeLibraries;
                var assembly = Assembly.Load(new AssemblyName(rootAssemblyName));
                assemblies.Add(assembly);
                foreach (var library in dependencies)
                {
                    if (IsCandidateCompilationLibrary(library, rootAssemblyName))
                    {
                        assembly = Assembly.Load(new AssemblyName(library.Name));
                        assemblies.Add(assembly);
                    }
                }
                return assemblies;
            }

            private static bool IsCandidateCompilationLibrary(RuntimeLibrary compilationLibrary, string rootAssemblyName)
            {
                return compilationLibrary.Dependencies.Any(d => d.Name.StartsWith(rootAssemblyName));
            }
        }
        
        public static IEnumerable<Type> Get<TMessage, TServiceMessage>(Func<Assembly, bool> assemblyFilter = null, Func<Type, bool> typeFilter = null)
            where TMessage : class
            where TServiceMessage : class
        {
            assemblyFilter = assemblyFilter ?? (assembly => true);
            typeFilter = typeFilter ?? (type => true);

            var types = AppDomainReplacement
                .GetAllAssembliesFromAppDomain()
                .Where(assemblyFilter)
                .SelectMany(GetTypes)
                .Where(typeFilter)
                .Where(t => t.GetTypeInfo().IsValidMessageType<TMessage, TServiceMessage>())
                .ToArray();

            EnsureTypesAreVisible(types);

            return types.ToArray();
        }

        private static void EnsureTypesAreVisible(IEnumerable<Type> types)
        {
            var notVisible = types.Where(t => !t.GetTypeInfo().IsVisible).ToArray();

            if (notVisible.Any())
            {
                throw new Exception(
                    "The following message types are not visible so Obvs will not be able to deserialize them. Please mark as public: " + Environment.NewLine +
                    string.Join(Environment.NewLine, notVisible.Select(t => string.Format("- {0}", t.FullName))));
            }
        }

        private static Type[] GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetExportedTypes();
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format(
                        "Error loading types from assembly '{0}', please consider using the provided configuration options to filter which assemblies you would like to load your message types from.",
                        assembly.FullName), exception);
            }
        }
    }
}