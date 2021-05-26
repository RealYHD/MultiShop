using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace MultiShop.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

            builder.RootComponents.Add<App>("#app");

            builder.Services.AddHttpClient("MultiShop.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)).AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("MultiShop.ServerAPI"));

            Action<HttpClient> configureClient = client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
            builder.Services.AddHttpClient("Public-MultiShop.ServerAPI", configureClient);

            IReadOnlyDictionary<string, string> webApiConfig = null;
            using (HttpClient client = new HttpClient())
            {
                configureClient.Invoke(client);
                webApiConfig = await client.GetFromJsonAsync<IReadOnlyDictionary<string, string>>("PublicApiSettings");
            }

            builder.Configuration.AddInMemoryCollection(webApiConfig);


            builder.Services.AddApiAuthorization();

            await builder.Build().RunAsync();
        }
    }
}
