using System;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;

namespace AutoNumber
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAutoNumber(this IServiceCollection services, IConfiguration configuration,
            Func<AutoNumberOptionsBuilder, AutoNumberOptions> builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var builderOptions = new AutoNumberOptionsBuilder(configuration);
            var options = builder(builderOptions);

            services.AddSingleton<IOptimisticDataStore, BlobOptimisticDataStore>(x =>
            {
                CloudStorageAccount storageAccount;

                if (options.CloudStorageAccount != null)
                    storageAccount = options.CloudStorageAccount;
                else if (options.StorageAccountConnectionString == null)
                    storageAccount = x.GetService<CloudStorageAccount>();
                else
                    storageAccount = CloudStorageAccount.Parse(options.StorageAccountConnectionString);

                return new BlobOptimisticDataStore(storageAccount, options.StorageContainerName);
            });

            services.AddSingleton<IUniqueIdGenerator, UniqueIdGenerator>(x
                => new UniqueIdGenerator(x.GetService<IOptimisticDataStore>(), options));

            services.AddSingleton(options);

            return services;
        }
    }
}