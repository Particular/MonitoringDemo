namespace Particular.Aspire.ServicePlatform;

class ServicePlatformFileLicense(string licensePath) : ServicePlatformCachedLicense
{
    protected override string LoadLicenseText()
        => File.ReadAllText(licensePath);
}
