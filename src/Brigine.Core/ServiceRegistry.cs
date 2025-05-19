using System;
using System.Collections.Generic;

namespace Brigine.Core
{
    public class ServiceRegistry
    {
        private readonly Dictionary<Type, IFunctionProvider> _providers = new();
        private readonly DefaultFunctionProvider _defaultFunctionProvider = new DefaultFunctionProvider();
        
        public void RegisterFunctionProvider(IFunctionProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
                
            Type providerType = provider.GetType();
            _providers[providerType] = provider;
        }

        public T GetService<T>() where T : class
        {
            foreach (var provider in _providers.Values)
            {
                var service = provider.GetService<T>();
                if (service != null)
                {
                    return service;
                }
            }
            return _defaultFunctionProvider.GetService<T>();
        }
    }

    public interface IFunctionProvider
    {
        T GetService<T>() where T : class;
    }
}