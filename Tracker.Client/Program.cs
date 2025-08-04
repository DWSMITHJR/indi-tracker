using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Tracker.Client;
using Tracker.Client.Services;
using Tracker.Shared.Services;
using Blazored.LocalStorage;
using Blazored.Toast;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register Blazored services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();

// Register application services
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IIndividualService, IndividualService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IToastService, ToastService>();

// Register log service
builder.Services.AddScoped<Tracker.Client.Services.ILogService, LogService>();

// Register auth services
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

await builder.Build().RunAsync();
