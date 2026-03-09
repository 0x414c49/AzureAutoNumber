using System.IO;
using System.Threading.Tasks;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.Azurite;
using Xunit;

namespace AutoNumber.IntegrationTests
{
    [Collection("Azurite")]
    public class DependencyInjectionFixture : IAsyncLifetime
    {
        private readonly AzuriteContainer _azuriteContainer;
        public string ConnectionString { get; private set; }
        public IConfigurationRoot Configuration { get; }

        public DependencyInjectionFixture()
        {
            _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
                .WithCommand("--skipApiVersionCheck")
                .Build();

            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _azuriteContainer.StartAsync();
            ConnectionString = _azuriteContainer.GetConnectionString();
        }

        public async Task DisposeAsync()
        {
            await _azuriteContainer.DisposeAsync();
        }
    }

    [Collection("Azurite")]
    public class DependencyInjectionTests : IClassFixture<DependencyInjectionFixture>
    {
        private readonly DependencyInjectionFixture _fixture;
        private const int CustomBatchSize = 5;
        private const int CustomMaxWriteAttempts = 10;
        private const string DefaultContainerName = "unique-urls";
        private const string CustomContainerName = "test";
        private const string TestConnectionString = "test123";
        private const string AlternateConnectionString = "test-22";
        private const int ExpectedMaxWriteAttempts = 25;
        private const int ExpectedBatchSize = 50;
        private const int CustomBatchSizeForGenerator = 10;

        public DependencyInjectionTests(DependencyInjectionFixture fixture)
        {
            _fixture = fixture;
        }

        private ServiceProvider GenerateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new BlobServiceClient(_fixture.ConnectionString));
            serviceCollection.AddSingleton<IConfiguration>(_fixture.Configuration);
            serviceCollection.AddAutoNumber();
            return serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void OptionsBuilderShouldGenerateOptions()
        {
            var serviceProvider = GenerateServiceProvider();
            var optionsBuilder = new AutoNumberOptionsBuilder(serviceProvider.GetService<IConfiguration>());

            optionsBuilder.SetBatchSize(CustomBatchSize);
            Assert.Equal(CustomBatchSize, optionsBuilder.Options.BatchSize);

            optionsBuilder.SetMaxWriteAttempts(CustomMaxWriteAttempts);
            Assert.Equal(CustomMaxWriteAttempts, optionsBuilder.Options.MaxWriteAttempts);

            optionsBuilder.UseDefaultContainerName();
            Assert.Equal(DefaultContainerName, optionsBuilder.Options.StorageContainerName);

            optionsBuilder.UseContainerName(CustomContainerName);
            Assert.Equal(CustomContainerName, optionsBuilder.Options.StorageContainerName);

            optionsBuilder.UseDefaultStorageAccount();
            Assert.Null(optionsBuilder.Options.StorageAccountConnectionString);

            optionsBuilder.UseStorageAccount(CustomContainerName);
            Assert.Equal(TestConnectionString, optionsBuilder.Options.StorageAccountConnectionString);

            optionsBuilder.UseStorageAccount(AlternateConnectionString);
            Assert.Equal(AlternateConnectionString, optionsBuilder.Options.StorageAccountConnectionString);
        }

        [Fact]
        public void ShouldCraeteUniqueIdGenerator()
        {
            var serviceProvider = GenerateServiceProvider();

            var uniqueId = serviceProvider.GetService<IUniqueIdGenerator>();

            Assert.NotNull(uniqueId);
        }

        [Fact]
        public void ShouldOptionsContainsDefaultValues()
        {
            var serviceProvider = GenerateServiceProvider();

            var options = serviceProvider.GetService<IOptions<AutoNumberOptions>>();

            Assert.NotNull(options.Value);
            Assert.Equal(ExpectedMaxWriteAttempts, options.Value.MaxWriteAttempts);
            Assert.Equal(ExpectedBatchSize, options.Value.BatchSize);
            Assert.Equal(DefaultContainerName, options.Value.StorageContainerName);
        }

        [Fact]
        public void ShouldResolveUniqueIdGenerator()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new BlobServiceClient(_fixture.ConnectionString));

            serviceCollection.AddAutoNumber(_fixture.Configuration, x =>
            {
                return x.UseContainerName("ali")
                    .UseDefaultStorageAccount()
                    .SetBatchSize(CustomBatchSizeForGenerator)
                    .SetMaxWriteAttempts()
                    .Options;
            });

            var service = serviceCollection.BuildServiceProvider()
                .GetService<IUniqueIdGenerator>();

            Assert.NotNull(service);
        }
    }
}
