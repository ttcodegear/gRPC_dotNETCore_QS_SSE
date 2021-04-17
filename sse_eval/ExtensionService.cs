using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Qlik.Sse;
using Google.Protobuf;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace SSE_Example
{
    public class ExtensionService : Connector.ConnectorBase
    {
        private readonly ILogger<ExtensionService> _logger;
        public ExtensionService(ILogger<ExtensionService> logger)
        {
            _logger = logger;
        }

        private ScriptOptions GetScriptOptions()
        {
            var opt = ScriptOptions.Default;
            var mscorlib = typeof(Object).Assembly;
            var systemCore = typeof(System.Linq.Enumerable).Assembly;
            var references = new[] { mscorlib, systemCore };
            opt = opt.AddReferences(references);
            opt = opt.AddImports("System");
            opt = opt.AddImports("System.Collections.Generic");
            opt = opt.AddImports("System.Linq");
            return opt;
        }

        public class ParamModel
        {
           public List<Object> Args { get; set; }
        }

        // Function Name | Function Type  | Argument     | TypeReturn Type
        // ScriptEval    | Scalar, Tensor | Numeric      | Numeric
        // ScriptEvalEx  | Scalar, Tensor | Dual(N or S) | Numeric
        private async Task ScriptEval(ScriptRequestHeader header, IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("script=" + header.Script);
            // パラメータがあるか否かをチェック
            if( header.Params.Count() > 0 ) {
                await foreach(var bundled_rows in requestStream.ReadAllAsync()) {
                    var all_args = new List<List<Object>>();
                    foreach(var row in bundled_rows.Rows) {
                        var script_args = new List<Object>();
                        var zip = header.Params.Zip(row.Duals, (p, d) => new { DataType = p.DataType, Dual = d });
                        foreach(var elm in zip) {
                            if( elm.DataType == DataType.Numeric || elm.DataType == DataType.Dual )
                                script_args.Add(elm.Dual.NumData);
                            else
                                script_args.Add(elm.Dual.StrData);
                        }
                        _logger.LogInformation("args=" + string.Join(",", script_args));
                        all_args.Add(script_args);
                    }
                    var all_results = new List<Object>();
                    foreach(var script_args in all_args) {
                        Object result = null;
                        try {
                            var model = new ParamModel { Args = script_args };
                            var opt = GetScriptOptions();
                            var state = CSharpScript.RunAsync(header.Script, opt, model, model.GetType()).Result;
                            result = state.Variables.Single(v => v.Name == "result").Value;
                        }
                        catch(Exception ex) {
                            _logger.LogInformation(ex.Message);
                        }
                        all_results.Add(result);
                    }
                    var response_rows = new BundledRows();
                    foreach(var result in all_results) {
                        var duals = new Row();
                        if(result is double)
                            duals.Duals.Add(new Dual{ NumData = (double)result });
                        else if(result is string)
                            duals.Duals.Add(new Dual{ NumData = double.Parse((string)result) });
                        else
                            duals.Duals.Add(new Dual{ NumData = Double.NaN });
                        response_rows.Rows.Add(duals);
                    }
                    await responseStream.WriteAsync(response_rows);
                }
            }
            else {
                var script_args = new List<Object>();
                Object result = null;
                try {
                    var model = new ParamModel { Args = script_args };
                    var opt = GetScriptOptions();
                    var state = CSharpScript.RunAsync(header.Script, opt, model, model.GetType()).Result;
                    result = state.Variables.Single(v => v.Name == "result").Value;
                }
                catch(Exception ex) {
                    _logger.LogInformation(ex.Message);
                }
                var response_rows = new BundledRows();
                var duals = new Row();
                if(result is double)
                    duals.Duals.Add(new Dual{ NumData = (double)result });
                else if(result is string)
                    duals.Duals.Add(new Dual{ NumData = double.Parse((string)result) });
                else
                    duals.Duals.Add(new Dual{ NumData = Double.NaN });
                response_rows.Rows.Add(duals);
                await responseStream.WriteAsync(response_rows);
            }
        }

        // Function Name   | Function Type | Argument     | TypeReturn Type
        // ScriptAggrStr   | Aggregation   | String       | String
        // ScriptAggrExStr | Aggregation   | Dual(N or S) | String
        private async Task ScriptAggrStr(ScriptRequestHeader header, IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("script=" + header.Script);
            // パラメータがあるか否かをチェック
            if( header.Params.Count() > 0 ) {
                var all_args = new List<Object>();
                await foreach(var bundled_rows in requestStream.ReadAllAsync()) {
                    foreach(var row in bundled_rows.Rows) {
                        var script_args = new List<Object>();
                        var zip = header.Params.Zip(row.Duals, (p, d) => new { DataType = p.DataType, Dual = d });
                        foreach(var elm in zip) {
                            if( elm.DataType == DataType.String || elm.DataType == DataType.Dual )
                                script_args.Add(elm.Dual.StrData);
                            else
                                script_args.Add(elm.Dual.NumData);
                        }
                        all_args.Add(script_args);
                    }
                }
                string log_args = "args=|";
                all_args.ForEach(e => log_args += string.Join(",", (List<Object>)e) + "|");
                _logger.LogInformation(log_args);
                Object result = null;
                try {
                    var model = new ParamModel { Args = all_args };
                    var opt = GetScriptOptions();
                    var state = CSharpScript.RunAsync(header.Script, opt, model, model.GetType()).Result;
                    result = state.Variables.Single(v => v.Name == "result").Value;
                }
                catch(Exception ex) {
                    _logger.LogInformation(ex.Message);
                }
                var response_rows = new BundledRows();
                var duals = new Row();
                if(result is string)
                    duals.Duals.Add(new Dual{ StrData = (string)result });
                else if(result is double)
                    duals.Duals.Add(new Dual{ StrData = ((double)result).ToString() });
                else
                    duals.Duals.Add(new Dual{ StrData = "" });
                response_rows.Rows.Add(duals);
                await responseStream.WriteAsync(response_rows);
            }
            else {
                var script_args = new List<Object>();
                Object result = null;
                try {
                    var model = new ParamModel { Args = script_args };
                    var opt = GetScriptOptions();
                    var state = CSharpScript.RunAsync(header.Script, opt, model, model.GetType()).Result;
                    result = state.Variables.Single(v => v.Name == "result").Value;
                }
                catch(Exception ex) {
                    _logger.LogInformation(ex.Message);
                }
                var response_rows = new BundledRows();
                var duals = new Row();
                if(result is string)
                    duals.Duals.Add(new Dual{ StrData = (string)result });
                else if(result is double)
                    duals.Duals.Add(new Dual{ StrData = ((double)result).ToString() });
                else
                    duals.Duals.Add(new Dual{ StrData = "" });
                response_rows.Rows.Add(duals);
                await responseStream.WriteAsync(response_rows);
            }
        }

        private string GetFunctionName(ScriptRequestHeader header)
        {
            var func_type = header.FunctionType;
            IEnumerable<DataType> arg_types = header.Params.Select(param => param.DataType);
            var ret_type  = header.ReturnType;
/*
            if( func_type == FunctionType.Scalar || func_type == FunctionType.Tensor )
                _logger.LogInformation("func_type SCALAR TENSOR");
            else if( func_type == FunctionType.Aggregation )
                _logger.LogInformation("func_type AGGREGATION");

            if( arg_types.Count() == 0 )
                _logger.LogInformation("arg_type Empty");
            else if( arg_types.All(a => a == DataType.Numeric) )
                _logger.LogInformation("arg_type NUMERIC");
            else if( arg_types.All(a => a == DataType.String) )
                _logger.LogInformation("arg_type STRING");
            else if( arg_types.ToHashSet().Count() >= 2 || arg_types.All(a => a == DataType.Dual) )
                _logger.LogInformation("arg_type DUAL");

            if( ret_type == DataType.Numeric )
                _logger.LogInformation("ret_type NUMERIC");
            else if( ret_type == DataType.String )
                _logger.LogInformation("ret_type STRING");
*/
            if( func_type == FunctionType.Scalar || func_type == FunctionType.Tensor )
                if( arg_types.Count() == 0 || arg_types.All(a => a == DataType.Numeric) )
                    if( ret_type == DataType.Numeric )
                        return "ScriptEval";
            
            if( func_type == FunctionType.Scalar || func_type == FunctionType.Tensor )
                if( arg_types.ToHashSet().Count() >= 2 || arg_types.All(a => a == DataType.Dual) )
                    if( ret_type == DataType.Numeric )
                        return "ScriptEvalEx";
            
            if( func_type == FunctionType.Aggregation )
                if( arg_types.Count() == 0 || arg_types.All(a => a == DataType.String) )
                    if( ret_type == DataType.String )
                        return "ScriptAggrStr";
            
            if( func_type == FunctionType.Aggregation )
                if( arg_types.ToHashSet().Count() >= 2 || arg_types.All(a => a == DataType.Dual) )
                    if( ret_type == DataType.String )
                        return "ScriptAggrExStr";
            
            return "Unsupported Function Name";
        }

        public override async Task EvaluateScript(IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)
        {
            context.ResponseTrailers.Add("qlik-cache", "no-store"); // Disable caching
            
            // Read gRPC metadata
            var entry = context.RequestHeaders.Single(entry => entry.Key == "qlik-scriptrequestheader-bin");
            var header = new ScriptRequestHeader();
            header.MergeFrom(new CodedInputStream(entry.ValueBytes));
            var func_name = GetFunctionName(header);
            if(func_name.Equals("ScriptEval") || func_name.Equals("ScriptEvalEx"))
                await ScriptEval(header, requestStream, responseStream, context);
            else if(func_name.Equals("ScriptAggrStr") || func_name.Equals("ScriptAggrExStr"))
                await ScriptAggrStr(header, requestStream, responseStream, context);
            else
                throw new RpcException(new Status(StatusCode.Unimplemented, "Method not implemented!"));
        }

        public override Task<Capabilities> GetCapabilities(Empty request, ServerCallContext context)
        {
            var capabilities = new Capabilities();
            capabilities.AllowScript = true;
            capabilities.PluginIdentifier = "Simple SSE Test";
            capabilities.PluginVersion = "v0.0.1";
            return Task.FromResult(capabilities);
        }
    }
}
