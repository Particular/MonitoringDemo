using ClientUI;
using Messages;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNServiceBus("ClientUI", (_, routing) => routing.RouteToEndpoint(typeof(PlaceOrder), "Sales"));

builder.Services.AddSingleton<SimulatedCustomers>();
builder.Services.AddHostedService<SimulateCustomersBackgroundService>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
