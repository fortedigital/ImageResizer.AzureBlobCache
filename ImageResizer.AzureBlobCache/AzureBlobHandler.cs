using System.IO;
using System.Linq;
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
        private static readonly int[] ignoredHttpExceptionCodes = {-2147023667, -2147024809, -2147023901};

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
            catch (HttpException e) when (ignoredHttpExceptionCodes.Contains(e.ErrorCode))
            {
                //ignoring exception The Remote host closed the connection. The error code is X.
                //where X might be: 0x800703E3, 0x80070057, 0x800704CD
                //it happens when request is aborted by client
            }

            this.data.Dispose();
        }
    }
}
