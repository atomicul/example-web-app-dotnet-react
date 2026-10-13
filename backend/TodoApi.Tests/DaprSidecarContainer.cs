using System.Net;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

[assembly: AssemblyFixture(typeof(TodoApi.Tests.DaprSidecarContainer))]

namespace TodoApi.Tests;

/// <summary>
/// Runs a Dapr sidecar in Docker, shared by every test in the run, whose output bindings are of the provider-agnostic
/// type <c>bindings.localstorage</c>: each writes every message it gets to a file in its own directory on this machine,
/// so tests can read what the API sent through Dapr. Those directories are made writable by anyone beforehand, since
/// daprd runs as its own user, which also leaves this machine able to delete what it writes.
/// </summary>
public class DaprSidecarContainer : IAsyncLifetime
{
    public const string EmailsBinding = "emails";

    private const int GrpcPort = 50001;
    private const int HttpPort = 3500;
    private const string ComponentsPath = "/components";
    private const string MessagesPath = "/messages";

    private readonly DirectoryInfo messages = Directory.CreateTempSubdirectory();
    private readonly IContainer container;

    public DaprSidecarContainer()
    {
        var emails = messages.CreateSubdirectory(EmailsBinding);
        if (!OperatingSystem.IsWindows())
        {
            messages.UnixFileMode = AllCanReadWriteAndSearch;
            emails.UnixFileMode = AllCanReadWriteAndSearch;
        }
        container = new ContainerBuilder("daprio/daprd:1.18.4")
            .WithCommand(
                "/daprd",
                "--app-id", "todoapp",
                "--resources-path", ComponentsPath,
                "--dapr-grpc-port", $"{GrpcPort}",
                "--dapr-http-port", $"{HttpPort}")
            .WithResourceMapping(LocalStorageBinding(EmailsBinding), $"{ComponentsPath}/{EmailsBinding}.yaml")
            .WithBindMount(messages.FullName, MessagesPath)
            .WithPortBinding(GrpcPort, assignRandomHostPort: true)
            .WithPortBinding(HttpPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort(HttpPort)
                    .ForPath("/v1.0/healthz/outbound")
                    .ForStatusCode(HttpStatusCode.NoContent)))
            .Build();
    }

    /// <summary>The address to set as <c>Dapr:GrpcEndpoint</c>.</summary>
    public string GrpcEndpoint => $"http://{container.Hostname}:{container.GetMappedPublicPort(GrpcPort)}";

    /// <summary>Every message the binding has been sent so far, read as JSON.</summary>
    public IEnumerable<T> MessagesSentTo<T>(string binding)
    {
        var directory = new DirectoryInfo(Path.Join(messages.FullName, binding));
        return directory.Exists
            ? directory.EnumerateFiles().Select(file => JsonSerializer.Deserialize<T>(File.ReadAllText(file.FullName),
                JsonSerializerOptions.Web)!)
            : [];
    }

    public async ValueTask InitializeAsync() => await container.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
        messages.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const UnixFileMode AllCanReadWriteAndSearch =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
        UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
        UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

    private static byte[] LocalStorageBinding(string name) => Encoding.UTF8.GetBytes($"""
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
          name: {name}
        spec:
          type: bindings.localstorage
          version: v1
          metadata:
            - name: rootPath
              value: {MessagesPath}/{name}
        """);
}
