namespace Particular.Aspire.ServicePlatform;

abstract class ServicePlatformCachedLicense : ServicePlatformLicense
{
    string? licenseText;

    protected override string GetLicenseText()
    {
        return licenseText ??= LoadLicenseText();
    }

    protected abstract string LoadLicenseText();
}