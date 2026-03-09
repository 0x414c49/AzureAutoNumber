# Azure AutoNumber

---

[![.NET Build](https://github.com/0x414c49/AzureAutoNumber/actions/workflows/dotnet.yml/badge.svg)](https://github.com/0x414c49/AzureAutoNumber/actions)
[![Build Status](https://img.shields.io/github/license/0x414c49/AzureAutoNumber)]()
[![NuGet version (AzureAutoNumber)](https://img.shields.io/nuget/v/AzureAutoNumber.svg?style=flat-square)](https://www.nuget.org/packages/AzureAutoNumber/)

High performance, distributed unique thread-safe id generator for Azure.

- Human-friendly generated ids (number)
- High performant and fast
- 100% guarantee that won't cause any duplicate ids
- Supports .NET 8.0 and .NET 10.0
- Modern Azure SDK (Azure.Storage.Blobs 12.27.0)
- Central Package Management

## How to use

The project relies on Azure Blob Storage. `AutoNumber` package will generate ids by using a single text file on the Azure Blob Storage.


```
var blobServiceClient = new BlobServiceClient(connectionString);

var blobOptimisticDataStore = new BlobOptimisticDataStore(blobServiceClient, "unique-ids");

var idGen = new UniqueIdGenerator(blobOptimisticDataStore);

// generate ids with different scopes

var id = idGen.NextId("urls");
var id2 = idGen.NextId("orders");
```

### With Microsoft DI
The project has an extension method to add it and its dependencies to Microsoft ASP.NET DI.


Use options builder to configure the service, take into account the default settings will read from `appsettings.json`.

```
services.AddAutoNumber(Configuration, x =>
{
	return x.UseContainerName("container-name")
	 .UseStorageAccount("connection-string-or-connection-string-name")
   //.UseBlobServiceClient(blobServiceClient)
	 .SetBatchSize(10)
	 .SetMaxWriteAttempts(100)
	 .Options;
});
```

#### Inject `IUniqueIdGenerator` in constructor

```
public class Foo
{
  public Foo(IUniqueIdGenerator idGenerator)
  {
      _idGenerator = idGenerator;
  }
}
```

### Configuration
These are default configuration for `AutoNumber`. These options can be set via `appsettings.json`.

```
{
  "AutoNumber": {
    "BatchSize": 50,
    "MaxWriteAttempts": 25,
    "StorageContainerName": "unique-urls"
  }
}
```

## Development

### Requirements
- .NET 8.0 SDK or .NET 10.0 SDK
- Docker (for integration tests)

### Building
```bash
dotnet build --configuration Release
```

### Running Tests
```bash
# Run all tests
dotnet test --configuration Release

# Run only unit tests
dotnet test UnitTests/UnitTests.csproj --configuration Release

# Run integration tests (requires Docker)
dotnet test IntegrationTests/IntegrationTests.csproj --configuration Release
```

### Test Framework
- **Unit Tests:** xUnit 2.9.3
- **Integration Tests:** xUnit 2.9.3 with TestContainers.Azurite
- **Mocking:** NSubstitute 5.3.0

### Central Package Management
This project uses Central Package Management (CPM). All NuGet package versions are managed in `Directory.Packages.props` at the root level.

## Support
Support this project and me via [PayPal](https://paypal.me/alibahraminezhad)


## Credits
Most of the credits of this library goes to [Tatham Oddie](https://tatham.blog/2011/07/14/released-snowmaker-a-unique-id-generator-for-azure-or-any-other-cloud-hosting-environment/) for making SnowMaker. I forked his work and made lots of changes to modernize it with the latest .NET versions and Azure SDK.
