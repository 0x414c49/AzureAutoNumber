using Microsoft.Extensions.Configuration;
using System;

namespace AutoNumber.Options
{
    public class AutoNumberOptionsBuilder
    {
        private readonly IConfiguration _configuration;
        private const string DefaultContainerName = "unique-urls";
        private const string AutoNumber = "AutoNumber";

        public AutoNumberOptions Options { get; } = new AutoNumberOptions();

        public AutoNumberOptionsBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
            configuration.GetSection(AutoNumber).Bind(Options);
        }

        /// <summary>
        /// Uses the default StorageAccount already defined in dependency injection
        /// </summary>
        public AutoNumberOptionsBuilder UseDefaultStorageAccount()
        {
            Options.StorageAccountConnectionString = null;
            return this;
        }

        /// <summary>
        /// Uses an Azure StorageAccount connection string to init the blob storage
        /// </summary>
        /// <param name="connectionStringOrName"></param>
        public AutoNumberOptionsBuilder UseStorageAccount(string connectionStringOrName)
        {
            if (string.IsNullOrEmpty(connectionStringOrName))
                throw new ArgumentNullException(nameof(connectionStringOrName));

            Options.StorageAccountConnectionString =
                _configuration.GetConnectionString(connectionStringOrName) ?? connectionStringOrName;

            return this;
        }

        /// <summary>
        /// Default container name to store latest generated id on Azure blob storage
        /// </summary>
        public AutoNumberOptionsBuilder UseDefaultContainerName()
        {
            Options.StorageContainerName = DefaultContainerName;
            return this;
        }

        /// <summary>
        /// Container name for storing latest generated id on Azure blob storage
        /// </summary>
        /// <param name="containerName"></param>
        public AutoNumberOptionsBuilder UseContainerName(string containerName)
        {
            Options.StorageContainerName = containerName;
            return this;
        }

        /// <summary>
        /// Max retrying to generate unique id
        /// </summary>
        /// <param name="attempts"></param>
        public AutoNumberOptionsBuilder SetMaxWriteAttempts(int attempts = 100)
        {
            Options.MaxWriteAttempts = attempts;
            return this;
        }

        /// <summary>
        /// BatchSize for id generation, higher the value more losing unused id
        /// </summary>
        /// <param name="batchSize"></param>
        public AutoNumberOptionsBuilder SetBatchSize(int batchSize = 100)
        {
            Options.BatchSize = batchSize;
            return this;
        }
    }
}
