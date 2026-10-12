using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;

namespace TodoApi;

/// <summary>
/// Encrypts the Data Protection keys at rest with the PFX certificate at <c>DataProtection:Certificate:Path</c>,
/// unlocked with <c>DataProtection:Certificate:Password</c>. Without a path, keys are stored unencrypted.
/// </summary>
public static class KeyRingCertificate
{
    private const string Section = "DataProtection:Certificate";

    public static IDataProtectionBuilder ProtectKeysWithConfiguredCertificate(
        this IDataProtectionBuilder builder, IConfiguration configuration)
    {
        var path = configuration[$"{Section}:Path"];
        if (string.IsNullOrEmpty(path))
        {
            return builder;
        }

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(path, configuration[$"{Section}:Password"]);
        return builder.ProtectKeysWithCertificate(certificate);
    }
}
