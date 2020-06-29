using AutoNumber.Interfaces;
using AutoNumber.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoNumber
{
    public static class ServiceCollectionExtensions
    {
        private const string AutoNumber = "AutoNumber";

        public static IServiceCollection AddAutoNumber(this IServiceCollection services)
        {
            services.AddOptions<AutoNumberOptions>()
                    .Configure<IConfiguration>((settings, configuration)
                        => configuration.GetSection(AutoNumber).Bind(settings));

            services.AddSingleton<IOptimisticDataStore, BlobOptimisticDataStore>();
            services.AddSingleton<IUniqueIdGenerator, UniqueIdGenerator>();

            return services;
        }
    }
}
