using ReverseProxyPoC;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransformFactory<CustomerProfileTransformFactory>();
    //.AddTransforms<CustomerProfileTransformProvider>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.MapSwagger();
app.UseSwaggerUI();
app.MapGet("/ping", (HttpContext httpContext) =>
{
    httpContext.Response.StatusCode = StatusCodes.Status200OK;
    return httpContext.Response.WriteAsync("pong");
}).WithOpenApi().WithName("Ping");
app.MapControllers();
app.MapReverseProxy();

app.Run();