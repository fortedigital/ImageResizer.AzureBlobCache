using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Logging;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Forte.ImageResizer.AzureBlobCache
{
    public class AzureBlobCachePlugin : IPlugin, IAsyncTyrantCache, ICache, ISimpleLogger
    {
        private const string ConfigurationSettingsPrefix = "azureBlobCache.";
        private ILogger Logger { get; set; }


        private AzureBlobCacheProvider cacheProvider;
        private Config config;

        public IPlugin Install(Config c)
        {
            this.config = c;

            this.InitializeLogger();

            this.InitializeCacheProvider();

            c.Plugins.add_plugin(this);

            return this;
        }

        private void InitializeCacheProvider()
        {
            var connectionString = this.GetConfigValue("connectionString");
            var connectionStringName = this.GetConfigValue("connectionStringName");
            var containerName = this.GetConfigValue("containerName") ?? "image-cache";

            if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(connectionStringName))
            {
                throw new InvalidOperationException("Either connectionString or connectionStringName must be set.");
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                var connectionStringEntry = WebConfigurationManager.ConnectionStrings[connectionStringName];
                if (connectionStringEntry == null)
                {
                    throw new InvalidOperationException($"Connection string '{connectionStringName}' does not exist.");
                }

                connectionString = connectionStringEntry.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException($"Connection string '{connectionStringName}' was empty.");
                }
            }

            var blobContainer = GetCloudBlobContainer(connectionString, containerName);
            blobContainer.CreateIfNotExists();
            this.cacheProvider = new AzureBlobCacheProvider(blobContainer, this);
        }

        private void InitializeLogger()
        {
            if (this.config.get(ConfigurationSettingsPrefix + "logging", false))
            {
                var loggerName = this.GetType().ToString();

                if (this.config.Plugins.LogManager != null)
                {
                    this.Logger = this.config.Plugins.LogManager.GetLogger(loggerName);
                }
                else
                {
                    this.config.Plugins.LoggingAvailable += delegate (ILogManager mgr)
                    {
                        if (this.Logger != null)
                        {
                            this.Logger = mgr.GetLogger(loggerName);
                        }
                    };
                }
            }
        }


        private string GetConfigValue(string name)
        {
            var configKey = ConfigurationSettingsPrefix + name;
            return this.config.get(configKey, null);
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public bool CanProcess(HttpContext current, IAsyncResponsePlan e)
        {
            return true;
        }

        public bool CanProcess(HttpContext current, IResponseArgs e)
        {
            return true;
        }

        public async Task ProcessAsync(HttpContext current, IAsyncResponsePlan e)
        {
            var blobName = ResolveBlobName(e.RequestCachingKey, e.EstimatedFileExtension);

            var cachedStream = await this.cacheProvider.ResolveAsync(blobName, async outputStream =>
            {
                await e.CreateAndWriteResultAsync(outputStream, e); // long-running call
            });

            Serve(current, e, cachedStream);
        }

        public void Process(HttpContext current, IResponseArgs e)
        {
            var blobName = ResolveBlobName(e.RequestKey, e.SuggestedExtension);

            var cachedStream = this.cacheProvider.Resolve(blobName, outputStream =>
            {
               e.ResizeImageToStream(outputStream); // long-running call
            });
            
            Serve(current, e, cachedStream);
        }

        private static void Serve(HttpContext context, IResponseArgs e, Stream data)
        {
            context.RemapHandler(new AzureBlobHandler(e, data));
        }

        private static void Serve(HttpContext context, IAsyncResponsePlan e, Stream data)
        {
            context.RemapHandler(new AzureBlobHandler(e, data));
        }

        private static string ResolveBlobName(string requestKey, string suggestedExtension)
        {
            return Sha256Hash.Compute(requestKey) + "." + suggestedExtension;
        }

        private static CloudBlobContainer GetCloudBlobContainer(string connectionString, string containerName)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var container = cloudBlobClient.GetContainerReference(containerName);
            return container;
        }

        public void LogError(string message, Exception exception)
        {
            this.Logger?.Error($"{message} {exception}");
        }
    }
}
