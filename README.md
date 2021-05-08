# gRPC_dotNETCore_QS_SSE
[simple]

$>dotnet --verson

3.1.408



$>dotnet new grpc -o greeter_server

$>cd greeter_server

[Protos/greet.proto]

---------

package helloworld;

---------

[Program.cs]

---------

using Microsoft.AspNetCore.Server.Kestrel.Core;

...

                    webBuilder.ConfigureKestrel(options =>

                    {

                        // Setup a HTTP/2 endpoint without TLS.

                        options.ListenAnyIP(50051, o => o.Protocols = HttpProtocols.Http2);

                    });

                    webBuilder.UseStartup<Startup>();

---------

$>dotnet build

$>dotnet run





$>dotnet new console -o greeter_client

$>cd greeter_client

$>dotnet add greeter_client.csproj package Grpc.Net.Client

$>dotnet add greeter_client.csproj package Google.Protobuf

$>dotnet add greeter_client.csproj package Grpc.Tools

$>mkdir Protos

$>cp ../greeter_server/Protos/greet.proto Protos

[greeter_client.csproj]

------

  <ItemGroup>

    <Protobuf Include="Protos\greet.proto" GrpcServices="Client" />

  </ItemGroup>

------

[Program.cs]

------

using System.Runtime.InteropServices;

using Grpc.Net.Client;

using greeter_server;

...

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var channel = GrpcChannel.ForAddress("http://localhost:50051");

            var client = new Greeter.GreeterClient(channel);

            var request = new HelloRequest();

            request.Name = "山田太郎";

            var reply = await client.SayHelloAsync(request);

            Console.WriteLine("Greeter client received: " + reply.Message);

------

$>dotnet build

$>dotnet run







[simple_ssl]

$>dotnet --verson

3.1.408



$>dotnet new grpc -o greeter_server

$>cd greeter_server

[Protos/greet.proto]

---------

package helloworld;

---------

[Program.cs]

---------

using Microsoft.AspNetCore.Server.Kestrel.Core;

using System.Net;

...

                    webBuilder.ConfigureKestrel(options => {

                      var pfxFilePath = "server.pfx";

                      var pfxPassword = "password"; 

                      options.Listen(IPAddress.Any, 50051, listenOptions => {

                        listenOptions.Protocols = HttpProtocols.Http2;

                        listenOptions.UseHttps(pfxFilePath, pfxPassword);

                      });

                    });

                    webBuilder.UseStartup<Startup>();

---------

$>openssl req -x509 -newkey rsa:4096 -sha256 -keyout server.key -out server.crt -subj "/CN=localhost" -days 3650 -nodes

$>openssl pkcs12 -export -name "localhost" -out server.pfx -inkey server.key -in server.crt

$>dotnet build

$>dotnet run





$>dotnet new console -o greeter_client

$>cd greeter_client

$>dotnet add greeter_client.csproj package Grpc.Net.Client

$>dotnet add greeter_client.csproj package Google.Protobuf

$>dotnet add greeter_client.csproj package Grpc.Tools

$>mkdir Protos

$>cp ../greeter_server/Protos/greet.proto Protos

[greeter_client.csproj]

------

  <ItemGroup>

    <Protobuf Include="Protos\greet.proto" GrpcServices="Client" />

  </ItemGroup>

------

[Program.cs]

------

using System.Runtime.InteropServices;

using System.Net.Http;

using Grpc.Net.Client;

using greeter_server;

...

            // Return `true` to allow certificates that are untrusted/invalid

            var httpHandler = new HttpClientHandler();

            httpHandler.ServerCertificateCustomValidationCallback =

                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var channel = GrpcChannel.ForAddress("https://jatok-tts1:50051",

                              new GrpcChannelOptions{HttpHandler=httpHandler});

            var client = new Greeter.GreeterClient(channel);

            var request = new HelloRequest();

            request.Name = "山田太郎";

            var reply = await client.SayHelloAsync(request);

            Console.WriteLine("Greeter client received: " + reply.Message);

------

$>dotnet build

$>dotnet run







[sse_eval]

$>dotnet --verson

3.1.408



$>dotnet new grpc -o SSE_Example

$>cd SSE_Example

$>dotnet add SSE_Example.csproj package Microsoft.CodeAnalysis.CSharp.Scripting

$>cp ../ServerSideExtension.proto Protos/

$>rm Protos/greet.proto

$>mv Services/GreeterService.cs Services/ExtensionService.cs

[SSE_Example.csproj]

---------

    <Protobuf Include="Protos\ServerSideExtension.proto" GrpcServices="Server" />

---------

[Program.cs]

---------

using Microsoft.AspNetCore.Server.Kestrel.Core;

...

                    webBuilder.ConfigureKestrel(options =>

                    {

                        // Setup a HTTP/2 endpoint without TLS.

                        options.ListenAnyIP(50053, o => o.Protocols = HttpProtocols.Http2);

                    });

                    webBuilder.UseStartup<Startup>();

---------

[Startup.cs]

---------

                endpoints.MapGrpcService<ExtensionService>();

---------

[ExtensionService.cs]

---------

using Qlik.Sse;

using Google.Protobuf;

using Microsoft.CodeAnalysis.CSharp.Scripting;

using Microsoft.CodeAnalysis.Scripting;

...

    public class ExtensionService : Connector.ConnectorBase

    {

        private readonly ILogger<ExtensionService> _logger;

        public ExtensionService(ILogger<ExtensionService> logger)

...

        public override async Task EvaluateScript(IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)

        {

            context.ResponseTrailers.Add("qlik-cache", "no-store"); // Disable caching

            ...

        }



        public override Task<Capabilities> GetCapabilities(Empty request, ServerCallContext context)

        {

            var capabilities = new Capabilities();

            capabilities.AllowScript = true;

            ...

            return Task.FromResult(capabilities);

        }

---------

$>dotnet build

$>dotnet run





C:\Users\[user]\Documents\Qlik\Sense\Settings.ini

------

[Settings 7]

SSEPlugin=Column,localhost:50053



------

