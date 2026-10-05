using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Dealoware.Infrastructure.Persistence;

namespace Dealoware.Api.Tests;

/// <summary>
/// Module initializer that creates a test certificate file for strict mode tests.
/// The DatabaseProviderSelector validates that the RDS root certificate bundle exists
/// in non-Development environments; this creates a valid test certificate at that path.
/// </summary>
public static class TestStartup
{
    private static bool _initialized;

    [ModuleInitializer]
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        CreateTestCertificateIfNeeded();
    }

    private static void CreateTestCertificateIfNeeded()
    {
        var certPath = DatabaseProviderSelector.RdsRootCertificatePath;
        
        try
        {
            var dir = Path.GetDirectoryName(certPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(certPath)) return;

            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=Test RDS CA, O=Test, C=US",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            req.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));

            using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(10));
            var pem = cert.ExportCertificatePem();
            File.WriteAllText(certPath, pem);
        }
        catch (UnauthorizedAccessException)
        {
            // Running in an environment where /app/certs is not writable.
            // Tests that require the cert will fail with a clear error message.
        }
        catch (IOException)
        {
            // Same - cert file could not be created.
        }
    }
}
