using System.Net.Sockets;
using Dapr.Client;
using Grpc.Net.Client;

namespace TodoApi;

/// <summary>
/// Connects the <see cref="DaprClient"/> to the Dapr sidecar at <c>Dapr:GrpcEndpoint</c>: either a
/// <c>unix:///path</c> to the gRPC socket of a sidecar on the same host, or an <c>http://</c> address. Without it, the
/// client falls back to Dapr's defaults. The client connects on its first call, so the app runs without a sidecar until
/// a feature configured to use Dapr sends something through it.
/// </summary>
public static class DaprSidecar
{
    public const string GrpcEndpointKey = "Dapr:GrpcEndpoint";

    public static IServiceCollection AddDaprSidecar(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDaprClient(builder => ConnectTo(builder, configuration[GrpcEndpointKey]));
        return services;
    }

    private static void ConnectTo(DaprClientBuilder builder, string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            return;
        }

        var address = new Uri(endpoint);
        if (address.Scheme != "unix")
        {
            builder.UseGrpcEndpoint(endpoint);
            return;
        }

        builder.UseGrpcEndpoint("http://localhost").UseGrpcChannelOptions(new GrpcChannelOptions
        {
            HttpHandler = UnixSocketHandler(address.LocalPath),
            ThrowOperationCanceledOnCancellation = true,
        });
    }

    private static SocketsHttpHandler UnixSocketHandler(string path) => new()
    {
        ConnectCallback = async (_, cancellationToken) =>
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };
}
