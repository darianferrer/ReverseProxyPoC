using System.Diagnostics;
using System.Net;
using ReverseProxyWithTransformations.Transformers;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<CustomerProfileLanguageTransformer>();

builder.Services.AddHttpForwarder();

var app = builder.Build();

// Configure our own HttpMessageInvoker for outbound calls for proxy operations
var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
{
    UseProxy = false,
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.None,
    UseCookies = false,
    ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
    ConnectTimeout = TimeSpan.FromSeconds(15),
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();

var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100) };

// When using IHttpForwarder for direct forwarding you are responsible for routing, destination discovery, load balancing, affinity, etc..
// For an alternate example that includes those features see BasicYarpSample.
app.Map("/Proxy/CustomerService/CustomerService.svc/json/GetCustomerPreferredLanguage", 
    async (HttpContext httpContext, IHttpForwarder forwarder, CustomerProfileLanguageTransformer transformer) =>
{
    var error = await forwarder.SendAsync(httpContext, "https://c99-customerprofile.service.ttlnonprod.local", httpClient, requestOptions,
        transformer);

    // Check if the proxy operation was successful
    if (error != ForwarderError.None)
    {
        var errorFeature = httpContext.Features.Get<IForwarderErrorFeature>();
        var exception = errorFeature.Exception;
    }
});
app.MapForwarder("/{**catch-all}", "https://c99-customer.service.ttlnonprod.local", requestOptions, HttpTransformer.Default, httpClient);

app.Run();