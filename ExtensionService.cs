using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Qlik.Sse;

namespace SSE_Example
{
    public class ExtensionService : Connector.ConnectorBase
    {
        private readonly ILogger<ExtensionService> _logger;
        public ExtensionService(ILogger<ExtensionService> logger)
        {
            _logger = logger;
        }

        public override Task<Capabilities> GetCapabilities(Empty request, ServerCallContext context)
        {
            var capabilities = new Capabilities();
            return Task.FromResult(capabilities);
        }

        public override async Task ExecuteFunction(IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)
        {
            await foreach(var request in requestStream.ReadAllAsync()) {
              //for(int i = 0; i < int.Parse(request.NumGreetings); i++) {
              //  var reply = new HelloReply();
              //  reply.Message = "‚±‚ñ‚É‚¿‚Í " + request.Name + " " + i;
              //  await responseStream.WriteAsync(reply);
              //}
            }
        }

        public override async Task EvaluateScript(IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)
        {
            await foreach(var request in requestStream.ReadAllAsync()) {
              //for(int i = 0; i < int.Parse(request.NumGreetings); i++) {
              //  var reply = new HelloReply();
              //  reply.Message = "‚±‚ñ‚É‚¿‚Í " + request.Name + " " + i;
              //  await responseStream.WriteAsync(reply);
              //}
            }
        }
    }
}
