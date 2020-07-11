using System.IO;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Microsoft.Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace AutoNumber.IntegrationTests
{
    [TestFixture]
    public class DependencyInjectionTest
    {
        public IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true).Build();

        private ServiceProvider GenerateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(CloudStorageAccount.DevelopmentStorageAccount);
            serviceCollection.AddSingleton<IConfiguration>(Configuration);
            serviceCollection.AddAutoNumber();
            return serviceCollection.BuildServiceProvider();
        }

        [Test]
        public void OptionsBuilderShouldGenerateOptions()
        {
            var serviceProvider = GenerateServiceProvider();
            var optionsBuilder = new AutoNumberOptionsBuilder(serviceProvider.GetService<IConfiguration>());

            optionsBuilder.SetBatchSize(5);
            Assert.AreEqual(5, optionsBuilder.Options.BatchSize);

            optionsBuilder.SetMaxWriteAttempts(10);
            Assert.AreEqual(10, optionsBuilder.Options.MaxWriteAttempts);

            optionsBuilder.UseDefaultContainerName();
            Assert.AreEqual("unique-urls", optionsBuilder.Options.StorageContainerName);

            optionsBuilder.UseContainerName("test");
            Assert.AreEqual("test", optionsBuilder.Options.StorageContainerName);

            optionsBuilder.UseDefaultStorageAccount();
            Assert.AreEqual(null, optionsBuilder.Options.StorageAccountConnectionString);

            optionsBuilder.UseStorageAccount("test");
            Assert.AreEqual("test123", optionsBuilder.Options.StorageAccountConnectionString);

            optionsBuilder.UseStorageAccount("test-22");
            Assert.AreEqual("test-22", optionsBuilder.Options.StorageAccountConnectionString);
        }

        [Test]
        public void ShouldCraeteUniqueIdGenerator()
        {
            var serviceProvider = GenerateServiceProvider();

            var uniqueId = serviceProvider.GetService<IUniqueIdGenerator>();

            Assert.NotNull(uniqueId);
        }

        [Test]
        public void ShouldOptionsContainsDefaultValues()
        {
            var serviceProvider = GenerateServiceProvider();

            var options = serviceProvider.GetService<IOptions<AutoNumberOptions>>();

            Assert.NotNull(options.Value);
            Assert.AreEqual(25, options.Value.MaxWriteAttempts);
            Assert.AreEqual(50, options.Value.BatchSize);
            Assert.AreEqual("unique-urls", options.Value.StorageContainerName);
        }

        [Test]
        public void ShouldResolveUniqueIdGenerator()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(CloudStorageAccount.DevelopmentStorageAccount);

            serviceCollection.AddAutoNumber(Configuration, x =>
            {
                return x.UseContainerName("ali")
                    .UseDefaultStorageAccount()
                    .SetBatchSize(10)
                    .SetMaxWriteAttempts()
                    .Options;
            });

            var service = serviceCollection.BuildServiceProvider()
                .GetService<IUniqueIdGenerator>();

            Assert.NotNull(service);
        }
    }
}