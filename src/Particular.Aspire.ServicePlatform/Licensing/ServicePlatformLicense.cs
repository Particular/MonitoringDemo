using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Particular.Aspire.ServicePlatform;

public abstract class ServicePlatformLicense
{
    public IResourceBuilder<ContainerResource> AddTo(IResourceBuilder<ContainerResource> builder)
        => builder.WithEnvironment("PARTICULARSOFTWARE_LICENSE", GetLicenseText());

    protected abstract string GetLicenseText();

    public static ServicePlatformLicense FromText(string licenseText)
        => new ServicePlatformTextLicense(licenseText);

    public static ServicePlatformLicense FromFile(string licensePath)
        => new ServicePlatformFileLicense(licensePath);

    public static ServicePlatformLicense CreateDefault()
        => new ServicePlatformDefaultLicense();
}
