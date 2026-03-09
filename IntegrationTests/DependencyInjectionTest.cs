using System.IO;
using System.Threading.Tasks;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Testcontainers.Azurite;

namespace AutoNumber.IntegrationTests
{
    [TestFixture]
    public class DependencyInjectionTest
    {
        private AzuriteContainer _azuriteContainer;
        private string _connectionString;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
                .WithCommand("--skipApiVersionCheck")
                .Build();
            
            await _azuriteContainer.StartAsync();
            
            _connectionString = _azuriteContainer.GetConnectionString();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_azuriteContainer != null)
            {
                await _azuriteContainer.DisposeAsync();
            }
        }

        public IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true).Build();

        private ServiceProvider GenerateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new BlobServiceClient(_connectionString));
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
            Assert.That(optionsBuilder.Options.BatchSize, Is.EqualTo(5));

            optionsBuilder.SetMaxWriteAttempts(10);
            Assert.That(optionsBuilder.Options.MaxWriteAttempts, Is.EqualTo(10));

            optionsBuilder.UseDefaultContainerName();
            Assert.That(optionsBuilder.Options.StorageContainerName, Is.EqualTo("unique-urls"));

            optionsBuilder.UseContainerName("test");
            Assert.That(optionsBuilder.Options.StorageContainerName, Is.EqualTo("test"));

            optionsBuilder.UseDefaultStorageAccount();
            Assert.That(optionsBuilder.Options.StorageAccountConnectionString, Is.EqualTo(null));

            optionsBuilder.UseStorageAccount("test");
            Assert.That(optionsBuilder.Options.StorageAccountConnectionString, Is.EqualTo("test123"));

            optionsBuilder.UseStorageAccount("test-22");
            Assert.That(optionsBuilder.Options.StorageAccountConnectionString, Is.EqualTo("test-22"));
        }

        [Test]
        public void ShouldCraeteUniqueIdGenerator()
        {
            var serviceProvider = GenerateServiceProvider();

            var uniqueId = serviceProvider.GetService<IUniqueIdGenerator>();

            Assert.That(uniqueId, Is.Not.Null);
        }

        [Test]
        public void ShouldOptionsContainsDefaultValues()
        {
            var serviceProvider = GenerateServiceProvider();

            var options = serviceProvider.GetService<IOptions<AutoNumberOptions>>();

            Assert.That(options.Value, Is.Not.Null);
            Assert.That(options.Value.MaxWriteAttempts, Is.EqualTo(25));
            Assert.That(options.Value.BatchSize, Is.EqualTo(50));
            Assert.That(options.Value.StorageContainerName, Is.EqualTo("unique-urls"));
        }

        [Test]
        public void ShouldResolveUniqueIdGenerator()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new BlobServiceClient(_connectionString));

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

            Assert.That(service, Is.Not.Null);
        }
    }
}