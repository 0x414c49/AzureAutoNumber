using System.IO;
using AutoNumber;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Xunit;

namespace IntegrationTests
{
    public class DependencyInjectionTest
    {
        private readonly IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true).Build();

        private ServiceProvider GenerateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddAutoNumber(configuration, builder
                => builder.UseStorageAccount(CloudStorageAccount.DevelopmentStorageAccount)
                    .Options);
            return serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void OptionsBuilder_Should_Generate_Options()
        {
            var serviceProvider = GenerateServiceProvider();
            var optionsBuilder = new AutoNumberOptionsBuilder(serviceProvider.GetService<IConfiguration>());

            optionsBuilder.SetBatchSize(5);
            Assert.Equal(5, optionsBuilder.Options.BatchSize);

            optionsBuilder.SetMaxWriteAttempts(10);
            Assert.Equal(10, optionsBuilder.Options.MaxWriteAttempts);

            optionsBuilder.UseDefaultContainerName();
            Assert.Equal("unique-urls", optionsBuilder.Options.StorageContainerName);

            optionsBuilder.UseContainerName("test");
            Assert.Equal("test", optionsBuilder.Options.StorageContainerName);

            optionsBuilder.UseDefaultStorageAccount();
            Assert.Null(optionsBuilder.Options.StorageAccountConnectionString);

            optionsBuilder.UseStorageAccount("test");
            Assert.Equal("test123", optionsBuilder.Options.StorageAccountConnectionString);

            optionsBuilder.UseStorageAccount("test-22");
            Assert.Equal("test-22", optionsBuilder.Options.StorageAccountConnectionString);
        }

        [Fact]
        public void Should_Create_Unique_IdGenerator()
        {
            var serviceProvider = GenerateServiceProvider();

            var uniqueId = serviceProvider.GetService<IUniqueIdGenerator>();

            Assert.NotNull(uniqueId);
        }

        [Fact]
        public void Should_Options_Contains_DefaultValues()
        {
            var serviceProvider = GenerateServiceProvider();

            var options = serviceProvider.GetService<AutoNumberOptions>();
            
            Assert.NotNull(options);
            Assert.Equal(25, options.MaxWriteAttempts);
            Assert.Equal(50, options.BatchSize);
            Assert.Equal("unique-urls", options.StorageContainerName);
        }

        [Fact]
        public void Should_Resolve_Unique_IdGenerator()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(CloudStorageAccount.DevelopmentStorageAccount);

            serviceCollection.AddAutoNumber(configuration, x
                => x.UseContainerName("ali")
                    .UseDefaultStorageAccount()
                    .SetBatchSize(10)
                    .SetMaxWriteAttempts()
                    .Options);

            var service = serviceCollection.BuildServiceProvider()
                .GetService<IUniqueIdGenerator>();

            Assert.NotNull(service);
        }
    }
}