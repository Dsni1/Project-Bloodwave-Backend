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
var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

app.Run();
