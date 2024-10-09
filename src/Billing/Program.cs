using Billing;
using Shared;

var builder = WebApplication.CreateBuilder();

builder.AddServiceDefaults();

builder.AddNServiceBus("Billing", (endpoint, _) => endpoint.LimitMessageProcessingConcurrencyTo(4));

builder.Services.AddSingleton<SimulationEffects>();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

