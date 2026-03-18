using Project_Bloodwave_Backend.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services
    .AddCorsPolicy()
    .AddDatabaseContext(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddSwaggerWithJwt()
    .AddApplicationServices();

builder.WebHost.UseUrls("http://0.0.0.0:5000");
//test
//test1
//test2
var app = builder.Build();

// Middleware
app.UseSwagger(options =>
{
    options.RouteTemplate = "api/docs/{documentName}/openapi.json";
});

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/docs";
    options.SwaggerEndpoint("/api/docs/v1/openapi.json", "Project Bloodwave API v1");
});

app.MapGet("/api", () => Results.Redirect("/api/docs"));

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
