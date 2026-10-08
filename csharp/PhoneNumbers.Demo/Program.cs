using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PhoneNumbers.Demo;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
// Injected rather than read directly so tests can drive the copy-confirmation timers.
builder.Services.AddSingleton(TimeProvider.System);

await builder.Build().RunAsync();
