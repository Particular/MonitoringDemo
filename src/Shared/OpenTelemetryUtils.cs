using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Shared;

public static class OpenTelemetryUtils
{
    public static IDisposable ConfigureOpenTelemetry(this EndpointConfiguration endpointConfig, string name, string id, int port)
    {
        var attributes = new Dictionary<string, object>
        {
            ["service.name"] = name,
            ["service.instance.id"] = id,
        };

        var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(attributes);

        var meterProviderBuilder = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter("NServiceBus.Core*");

        meterProviderBuilder.AddPrometheusHttpListener(options => options.UriPrefixes = new[] { $"http://127.0.0.1:{port}" });

        var meterProvider = meterProviderBuilder.Build();

        endpointConfig.EnableOpenTelemetry();

        return meterProvider;
    }
}