using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace MultiShop.Client
{
    public class RuntimeDependencyManager : IAsyncDisposable
    {
        private bool disposedValue;
        private HttpClient authenticatedHttp;
        private HttpClient publicHttp;
        private Task<AuthenticationState> authenticationStateTask;
        private ILogger logger;
        private Dictionary<Type, Dictionary<string, object>> RuntimeLoadedDependencies = new Dictionary<Type, Dictionary<string, object>>();

        public RuntimeDependencyManager(HttpClient publicHttp, HttpClient authenticatedHttp, Task<AuthenticationState> authenticationStateTask, ILogger logger)
        {
            this.publicHttp = publicHttp;
            this.authenticatedHttp = authenticatedHttp;
            this.authenticationStateTask = authenticationStateTask;
            this.logger = logger;
        }


        public async ValueTask SetupDependency(Dependency dependency)
        {
            logger.LogDebug($"Setting up dependency of type \"{dependency.Type}\" named \"{dependency.Name}\".");
            Dictionary<string, object> dependencies = RuntimeLoadedDependencies.GetValueOrDefault(dependency.Type, new Dictionary<string, object>());
            dependencies.Add(dependency.Name, await dependency.LoadDependency.Invoke(publicHttp, authenticatedHttp, await authenticationStateTask, logger));
            RuntimeLoadedDependencies[dependency.Type] = dependencies;
        }

        public T Get<T>(string name = "") {
            Type type = typeof(T);
            if (!RuntimeLoadedDependencies.ContainsKey(typeof(T))) throw new InvalidOperationException($"No dependency of type {type}.");
            if (!RuntimeLoadedDependencies[type].ContainsKey(name)) throw new InvalidOperationException($"No dependency of type {type} with name {name}.");
            return (T) RuntimeLoadedDependencies[type][name];
        }

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue) {
                foreach (Dictionary<string, object> dependencies in RuntimeLoadedDependencies.Values)
                {
                    foreach (object dependency in dependencies.Values)
                    {
                        IDisposable disposableDep = dependency as IDisposable;
                        if (disposableDep != null) {
                            disposableDep.Dispose();
                        } else {
                            IAsyncDisposable asyncDisposableDep = dependency as IAsyncDisposable;
                            if (asyncDisposableDep != null) {
                                await asyncDisposableDep.DisposeAsync();
                            }
                        }
                    }
                }
            }
            RuntimeLoadedDependencies.Clear();
            disposedValue = true;
        }


        public class Dependency
        {
            public Type Type { get; }
            public string Name { get; }
            public string DisplayName { get; }
            public Func<HttpClient, HttpClient, AuthenticationState, ILogger, ValueTask<object>> LoadDependency { get; }

            public Dependency(Type type, string displayName, Func<HttpClient, HttpClient, AuthenticationState, ILogger, ValueTask<object>> LoadDependencyFunc, string name = null)
            {
                this.Type = type;
                this.DisplayName = displayName;
                this.Name = name ?? "";
                this.LoadDependency = LoadDependencyFunc;
            }
        }
    }
}