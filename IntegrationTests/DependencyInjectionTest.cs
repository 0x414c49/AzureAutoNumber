using System.IO;
using AutoNumber.Interfaces;
using AutoNumber.Options;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace AutoNumber.IntegrationTests;

[TestFixture]
public class DependencyInjectionTest
{
    public IConfigurationRoot Configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", true, true).Build();

    private ServiceProvider GenerateServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(new BlobServiceClient("UseDevelopmentStorage=true"));
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
        Assert.Equals(5, optionsBuilder.Options.BatchSize);

        optionsBuilder.SetMaxWriteAttempts(10);
        Assert.Equals(10, optionsBuilder.Options.MaxWriteAttempts);

        optionsBuilder.UseDefaultContainerName();
        Assert.Equals("unique-urls", optionsBuilder.Options.StorageContainerName);

        optionsBuilder.UseContainerName("test");
        Assert.Equals("test", optionsBuilder.Options.StorageContainerName);

        optionsBuilder.UseDefaultStorageAccount();
        Assert.Equals(null, optionsBuilder.Options.StorageAccountConnectionString);

        optionsBuilder.UseStorageAccount("test");
        Assert.Equals("test123", optionsBuilder.Options.StorageAccountConnectionString);

        optionsBuilder.UseStorageAccount("test-22");
        Assert.Equals("test-22", optionsBuilder.Options.StorageAccountConnectionString);
    }

    [Test]
    public void ShouldCraeteUniqueIdGenerator()
    {
        var serviceProvider = GenerateServiceProvider();

        var uniqueId = serviceProvider.GetService<IUniqueIdGenerator>();

        uniqueId.Should().NotBeNull();
    }

    [Test]
    public void ShouldOptionsContainsDefaultValues()
    {
        var serviceProvider = GenerateServiceProvider();

        var options = serviceProvider.GetService<IOptions<AutoNumberOptions>>();

        options.Value.Should().NotBeNull();
        Assert.Equals(25, options.Value.MaxWriteAttempts);
        Assert.Equals(50, options.Value.BatchSize);
        Assert.Equals("unique-urls", options.Value.StorageContainerName);
    }

    [Test]
    public void ShouldResolveUniqueIdGenerator()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(new BlobServiceClient("UseDevelopmentStorage=true"));

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

        service.Should().NotBeNull();
    }
}