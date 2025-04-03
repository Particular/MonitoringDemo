namespace Particular.Aspire.ServicePlatform;

class ServicePlatformDefaultLicense : ServicePlatformCachedLicense
{
    protected override string LoadLicenseText()
        => MaybeReadLicense(Environment.GetEnvironmentVariable("PROGRAMDATA"))
        ?? MaybeReadLicense(Environment.GetEnvironmentVariable("LOCALAPPDATA"))
        ?? Environment.GetEnvironmentVariable("PARTICULARSOFTWARE_LICENSE")
        ?? throw new Exception("Particular license not found");

    static string? MaybeReadLicense(string? rootPath)
        => rootPath switch
        {
            null => null,
            _ => Path.Combine(rootPath, "ParticularSoftware", "license.xml") switch
            {
                var licensePath => File.Exists(licensePath)
                    ? File.ReadAllText(licensePath)
                    : null
            }
        };
}
