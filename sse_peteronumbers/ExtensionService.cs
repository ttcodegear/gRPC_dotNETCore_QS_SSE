using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Qlik.Sse;
using Google.Protobuf;
using PeterO.Numbers;

namespace SSE_Example
{
    public class ExtensionService : Connector.ConnectorBase
    {
        private readonly ILogger<ExtensionService> _logger;
        public ExtensionService(ILogger<ExtensionService> logger)
        {
            _logger = logger;
        }

        private async Task BigSum(IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("BigSum");
            var result = EDecimal.FromString("0");
            await foreach(var bundled_rows in requestStream.ReadAllAsync()) {
                foreach(var row in bundled_rows.Rows) {
                    result = result.Add(EDecimal.FromString(row.Duals[0].StrData)); // row=[Col1], Col1 + Col1 + ...
                }
            }
            var response_rows = new BundledRows();
            var duals = new Row();
            _logger.LogInformation(result.ToPlainString());
            duals.Duals.Add(new Dual{ StrData = result.ToPlainString() });
            response_rows.Rows.Add(duals);
            await responseStream.WriteAsync(response_rows);
        }

        private async Task BigAdd(IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("BigAdd");
            await foreach(var bundled_rows in requestStream.ReadAllAsync()) {
                var response_rows = new BundledRows();
                foreach(var row in bundled_rows.Rows) {
                    var result = EDecimal.FromString(row.Duals[0].StrData).Add(EDecimal.FromString(row.Duals[1].StrData)); // row=[Col1,Col2], sum=Col1 + Col2
                    var duals = new Row();
                    _logger.LogInformation(result.ToPlainString());
                    duals.Duals.Add(new Dual{ StrData = result.ToPlainString() });
                    response_rows.Rows.Add(duals);
                }
                await responseStream.WriteAsync(response_rows);
            }
        }

        private int GetFunctionId(ServerCallContext context)
        {
            // Read gRPC metadata
            var entry = context.RequestHeaders.Single(entry => entry.Key == "qlik-functionrequestheader-bin");
            var header = new FunctionRequestHeader();
            header.MergeFrom(new CodedInputStream(entry.ValueBytes));
            return header.FunctionId;
        }

        public override async Task ExecuteFunction(IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)
        {
            context.ResponseTrailers.Add("qlik-cache", "no-store"); // Disable caching
            
            var func_id = GetFunctionId(context);
            if(func_id == 0)
                await BigSum(requestStream, responseStream, context);
            else if(func_id == 1)
                await BigAdd(requestStream, responseStream, context);
            else
                throw new RpcException(new Status(StatusCode.Unimplemented, "Method not implemented!"));
        }

        public override Task<Capabilities> GetCapabilities(Empty request, ServerCallContext context)
        {
            var capabilities = new Capabilities();
            capabilities.AllowScript = false;
            capabilities.PluginIdentifier = "Simple SSE Test";
            capabilities.PluginVersion = "v0.0.1";
            
            // SumOfColumn
            var func0 = new FunctionDefinition();
            func0.FunctionId = 0;                          // 関数ID
            func0.Name = "BigSum";                         // 関数名
            func0.FunctionType = FunctionType.Aggregation; // 関数タイプ=0=スカラー,1=集計,2=テンソル
            func0.ReturnType = DataType.String;            // 関数戻り値=0=文字列,1=数値,2=Dual
            var func0_p1 = new Parameter();
            func0_p1.Name = "col1";                        // パラメータ名
            func0_p1.DataType = DataType.String;           // パラメータタイプ=0=文字列,1=数値,2=Dual
            func0.Params.Add(func0_p1);
            
            // SumOfRows
            var func1 = new FunctionDefinition();
            func1.FunctionId = 1;                          // 関数ID
            func1.Name = "BigAdd";                         // 関数名
            func1.FunctionType = FunctionType.Tensor;      // 関数タイプ=0=スカラー,1=集計,2=テンソル
            func1.ReturnType = DataType.String;            // 関数戻り値=0=文字列,1=数値,2=Dual
            var func1_p1 = new Parameter();
            func1_p1.Name = "col1";                        // パラメータ名
            func1_p1.DataType = DataType.String;           // パラメータタイプ=0=文字列,1=数値,2=Dual
            var func1_p2 = new Parameter();
            func1_p2.Name = "col2";                        // パラメータ名
            func1_p2.DataType = DataType.String;           // パラメータタイプ=0=文字列,1=数値,2=Dual
            func1.Params.Add(func1_p1);
            func1.Params.Add(func1_p2);
            
            capabilities.Functions.Add(func0);
            capabilities.Functions.Add(func1);
            return Task.FromResult(capabilities);
        }
    }
}
