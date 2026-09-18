var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/api/v1/health", () => Results.Ok(new
{
    status = "ok",
    message = "AlmacenCloud API is running"
}));

app.Run();
