namespace Particular.Aspire.ServicePlatform;

class ServicePlatformTextLicense(string licenseText) : ServicePlatformLicense
{
    protected override string GetLicenseText() => licenseText;
}
