using System;
using System.Collections.Generic;

namespace Brigine.Core
{
    public class ServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<Type, IFunctionProvider> _providers = new();

        public void RegisterProvider<T>(T provider) where T : IFunctionProvider
        {
            _providers[typeof(T)] = provider;
        }

        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;

            foreach (var provider in _providers.Values)
            {
                service = provider.GetService<T>();
                if (service != null)
                {
                    _services[typeof(T)] = service;
                    return (T)service;
                }
            }
            return (T)(_services[typeof(T)] = _providers[typeof(DefaultFunctionProvider)]?.GetService<T>());
        }
    }

    public interface IFunctionProvider
    {
        T GetService<T>() where T : class;
    }
}