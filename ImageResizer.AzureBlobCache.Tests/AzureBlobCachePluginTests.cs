using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web;
using ImageResizer;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using NUnit.Framework;

namespace Forte.ImageResizer.AzureBlobCache.Tests
{
    [TestFixture]
    public class AzureBlobCachePluginTests
    {
#if DEBUG
        private const string ConnectionStringName = "AzureStorageEmulator";
#else
        private const string ConnectionStringName = "AzureUnitTest";
#endif
        private static readonly string ConfigXml = 
            $@"<resizer>
                    <plugins>
                        <add name=""{typeof(AzureBlobCachePlugin)}""/>
                    </plugins>
                    <azureBlobCache connectionStringName=""{ConnectionStringName}""/>
                </resizer>";

        [Test]
        public void ShouldStoreImageInAzureBlob()
        {
            HttpContext.Current = new HttpContext(
                new HttpRequest(string.Empty, "http://tempuri.org", string.Empty),
                new HttpResponse(new StringWriter(CultureInfo.InvariantCulture)));

            // Arrange

            var config = new Config(new ResizerSection(ConfigXml));
            var plugin = config.Plugins.Get<AzureBlobCachePlugin>();

            var resizeCounter = 0;

            var settings = new ResizeSettings
            {
                Cache = ServerCacheMode.Default
            };
            var args = new ResponseArgs
            {
                RewrittenQuerystring = settings,
                RequestKey = "image.jpg?yy=123&xx="+DateTime.Now.Ticks,
                ResizeImageToStream = (Stream ms) =>
                {
                    Interlocked.Increment(ref resizeCounter);
                    ms.WriteByte(99);
                }
            };

            // Act
            plugin.Process(HttpContext.Current, args);
            plugin.Process(HttpContext.Current, args);

            // Assert
            Assert.That(resizeCounter, Is.EqualTo(1));
        }
    }
}