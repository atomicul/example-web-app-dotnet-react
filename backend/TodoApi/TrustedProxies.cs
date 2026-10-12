using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace TodoApi;

/// <summary>
/// Takes the client IP and scheme from <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> when a request comes
/// through a trusted proxy: loopback, plus the IP addresses in <c>TrustedProxies:Addresses</c> and the CIDR ranges in
/// <c>TrustedProxies:Networks</c>.
/// </summary>
public static class TrustedProxies
{
    private const string Section = "TrustedProxies";

    public static IServiceCollection AddTrustedProxies(this IServiceCollection services, IConfiguration configuration) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var address in ReadList(configuration, "Addresses"))
            {
                options.KnownProxies.Add(IPAddress.Parse(address));
            }
            foreach (var network in ReadList(configuration, "Networks"))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

    private static string[] ReadList(IConfiguration configuration, string key) =>
        configuration.GetSection($"{Section}:{key}").Get<string[]>() ?? [];
}
