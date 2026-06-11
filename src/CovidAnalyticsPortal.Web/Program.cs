using CovidAnalyticsPortal.Web.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Strongly-typed, validated configuration for the backend REST API.
builder.Services
    .AddOptions<CovidApiOptions>()
    .Bind(builder.Configuration.GetSection(CovidApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Typed HttpClient that consumes the backend COVID Analytics REST API.
builder.Services.AddHttpClient<ICovidApiClient, CovidApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<CovidApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
