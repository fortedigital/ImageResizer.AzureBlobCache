using System;

namespace Forte.ImageResizer.AzureBlobCache
{
    public interface ISimpleLogger
    {
        void LogError(string message, Exception exception);
    }
}