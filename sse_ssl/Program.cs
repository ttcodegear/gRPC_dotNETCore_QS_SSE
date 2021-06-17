using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace SSE_Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options => {
                      var pfxFilePath = "server.pfx";
                      var pfxPassword = "password"; 
                      options.Listen(IPAddress.Any, 50053, listenOptions => {
                        listenOptions.Protocols = HttpProtocols.Http2;
                        listenOptions.UseHttps(pfxFilePath, pfxPassword);
                      });
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
