using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Dealoware.Infrastructure.Persistence;

namespace Dealoware.Api.Tests;

/// <summary>
/// Module initializer that creates a test certificate file for strict mode tests.
/// The DatabaseProviderSelector validates that the RDS root certificate bundle exists
/// in non-Development environments; this creates a valid test certificate at a temp path
/// and sets the override environment variable.
/// </summary>
public static class TestStartup
{
    private static bool _initialized;
    private static string? _testCertPath;

    /// <summary>
    /// Path to the test certificate created by the module initializer.
    /// Null if initialization failed or hasn't run yet.
    /// </summary>
    public static string? TestCertificatePath => _testCertPath;

    [ModuleInitializer]
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        CreateTestCertificate();
    }

    private static void CreateTestCertificate()
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "dealoware-test-certs");
            Directory.CreateDirectory(tempDir);
            
            var certPath = Path.Combine(tempDir, "test-rds-ca.pem");
            _testCertPath = certPath;

            if (!File.Exists(certPath))
            {
                using var rsa = RSA.Create(2048);
                var req = new CertificateRequest(
                    "CN=Test RDS CA, O=Test, C=US",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                req.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(
                        certificateAuthority: true,
                        hasPathLengthConstraint: false,
                        pathLengthConstraint: 0,
                        critical: true));

                using var cert = req.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddYears(10));
                    
                var pem = cert.ExportCertificatePem();
                File.WriteAllText(certPath, pem);
            }

            // Set the environment variable so DatabaseProviderSelector uses this path
            Environment.SetEnvironmentVariable(
                DatabaseProviderSelector.RdsCertPathEnvVar,
                certPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"TestStartup: Failed to create test certificate: {ex.Message}");
            // Tests that require the cert will fail with a clear error message.
        }
    }
}
