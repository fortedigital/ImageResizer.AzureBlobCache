using System.IO;
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
            catch (HttpException e) when (e.ErrorCode == -2147023667)
            {
                //ignoring exception The Remote host closed the connection. The error code is 0x800704CD.
                //it happens when request is aborted by client
            }

            this.data.Dispose();
        }
    }
}
