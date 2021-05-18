using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using ImageResizer.Caching;
using ImageResizer.Plugins;
using ImageResizer.Util;

namespace Forte.ImageResizer.AzureBlobCache
{
    public class AzureBlobHandler : AsyncUtils.AsyncHttpHandlerBase
    {
        private readonly IResponseArgs syncResponse;
        private readonly IAsyncResponsePlan asyncResponse;
        private readonly Stream data;
        private const uint ERROR_CONNECTION_INVALID = 0x800704CD;
        private const uint ERROR_OPERATION_ABORTED = 0x800703E3;
        private const uint ERROR_INVALID_PARAMETER = 0x80070057;
        private const uint ERROR_BAD_COMMAND = 0x80070016;

        private static readonly uint[] ignoredHttpExceptionCodes =
        {
            ERROR_CONNECTION_INVALID,
            ERROR_OPERATION_ABORTED, 
            ERROR_INVALID_PARAMETER,
            ERROR_BAD_COMMAND,
        };

        public AzureBlobHandler(IResponseArgs syncResponse, Stream data)
        {
            this.syncResponse = syncResponse;
            this.data = data;
        }
        public AzureBlobHandler(IAsyncResponsePlan asyncResponse, Stream data)
        {
            this.asyncResponse = asyncResponse;
            this.data = data;
        }

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.StatusCode = 200;
            context.Response.BufferOutput = false;
            if (this.syncResponse != null)
            {
                this.syncResponse.ResponseHeaders.ApplyDuringPreSendRequestHeaders = false;
                this.syncResponse.ResponseHeaders.ApplyToResponse(this.syncResponse.ResponseHeaders, context);
            }
            if (this.asyncResponse != null)
            {
                context.Response.ContentType = this.asyncResponse.EstimatedContentType;
            }

            try
            {
                await this.data.CopyToAsync(context.Response.OutputStream);
            }
            catch (HttpException e) when (ignoredHttpExceptionCodes.Contains((uint) e.ErrorCode))
            {
                //ignoring exception The Remote host closed the connection. The error code is X.
                //it happens when request is aborted by client
            }
            catch (HttpException e) when (e.InnerException is COMException inner && ignoredHttpExceptionCodes.Contains((uint) inner.ErrorCode))
            {
                //ignoring exception An error occurred while communicating with the remote host. The error code is x.
                //it happens when client connection is disrupted before flushing the stream happens
            }
            finally
            {
                this.data.Dispose();
            }
        }
    }
}
