# ImageResizer AzureBlobCache

This is [ImageResizer](https://imageresizing.net) EpiServer plugin which stores resized images in Azure Blob Storage so consequent request for the same file does not trigger actual resize.

It covers the same functionality as [DiskCache](https://imageresizing.net/docs/v4/plugins/diskcache) plugin, which is a paid one.

## Installation

It is available as [nuget package](https://www.nuget.org/packages/Forte.ImageResizer.AzureBlobCache/)

## Configuration

Assuming you have your `ImageResizer` setup properly you only need to edit `web.config` and add one entry:

```xml
<resizer>
    <plugins>
      (...)
      <add name="Forte.ImageResizer.AzureBlobCache.AzureBlobCachePlugin" />
    </plugins>
    <!-- 
    There are two options to configure AzureBlobStorage connection:
    1. Most probably you already have your Azure Blob Storage connection 
    string somewhere so you may just pass its name in connectionStringName
    <azureBlobCache connectionStringName="EPiServerAzureBlobs"></azureBlobCache>
    
    2. If you don't have connection string defined elsewhere, you may 
    pass it in connectionString attribute 
    <azureBlobCache connectionString="YourConnectionString"></azureBlobCache>
    -->   
    <azureBlobCache connectionStringName="EPiServerAzureBlobs"></azureBlobCache>
 </resizer>
``` 
#### Unit tests

If you wish to run unit tests provided by this package you need to setup your own Azure Blob Storage account and edit `App.config` file
and replace `#{TestAzureBlobStorageConnectionString}#` with your own connection string.

