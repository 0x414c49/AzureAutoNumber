﻿using AzureHailstone.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;
using System.IO;

namespace AzureHailstone.IntegrationTests
{
    [TestFixture]
    public class DependencyInjectionTest
    {
        public IConfigurationRoot Configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

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

            var options = serviceProvider.GetService<IOptions<Options.HailstoneOptions>>();

            Assert.NotNull(options.Value);
            Assert.AreEqual(25, options.Value.MaxWriteAttempts);
            Assert.AreEqual(50, options.Value.BatchSize);
            Assert.AreEqual("unique-urls", options.Value.StorageContainerName);
        }


        private ServiceProvider GenerateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(CloudStorageAccount.DevelopmentStorageAccount);
            serviceCollection.AddSingleton<IConfiguration>(Configuration);
            serviceCollection.AddAzureHailstone();
            return serviceCollection.BuildServiceProvider();
        }
    }
}
