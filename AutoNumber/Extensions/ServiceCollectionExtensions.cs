using System;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoNumber
{
    public static class ServiceCollectionExtensions
    {
        private const string AutoNumber = "AutoNumber";

        [Obsolete("This method is deprecated, please use AddAutoNumber with options builder.", false)]
        public static IServiceCollection AddAutoNumber(this IServiceCollection services)
        {
            services.AddOptions<AutoNumberOptions>()
                .Configure<IConfiguration>((settings, configuration)
                    => configuration.GetSection(AutoNumber).Bind(settings));

            services.AddSingleton<IOptimisticDataStore, BlobOptimisticDataStore>();
            services.AddSingleton<IUniqueIdGenerator, UniqueIdGenerator>();

            return services;
        }

        public static IServiceCollection AddAutoNumber(this IServiceCollection services, IConfiguration configuration,
            Func<AutoNumberOptionsBuilder, AutoNumberOptions> builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var builderOptions = new AutoNumberOptionsBuilder(configuration);
            var options = builder(builderOptions);

            services.AddSingleton<IOptimisticDataStore, BlobOptimisticDataStore>(x =>
            {
                BlobServiceClient blobServiceClient = null;

                if (options.BlobServiceClient != null)
                    blobServiceClient = options.BlobServiceClient;
                else if (options.StorageAccountConnectionString == null)
                    blobServiceClient = x.GetService<BlobServiceClient>();
                else
                    blobServiceClient = new BlobServiceClient(options.StorageAccountConnectionString);

                return new BlobOptimisticDataStore(blobServiceClient, options.StorageContainerName);
            });

            services.AddSingleton<IUniqueIdGenerator, UniqueIdGenerator>(x
                => new UniqueIdGenerator(x.GetService<IOptimisticDataStore>(), options));

            return services;
        }
    }
}