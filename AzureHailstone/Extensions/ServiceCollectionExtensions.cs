using AzureHailstone.Interfaces;
using AzureHailstone.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureHailstone
{
    public static class ServiceCollectionExtensions
    {
        private const string AzureHailstone = "AzureHailstone";
        public static IServiceCollection AddAzureHailstone(this IServiceCollection services)
        {
            services.AddOptions<HailstoneOptions>()
                    .Configure<IConfiguration>((settings, configuration)
                        => configuration.GetSection(AzureHailstone).Bind(settings));

            services.AddScoped<IOptimisticDataStore, BlobOptimisticDataStore>();
            services.AddSingleton<IUniqueIdGenerator, UniqueIdGenerator>();

            return services;
        }
    }
}
