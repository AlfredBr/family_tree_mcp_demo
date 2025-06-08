var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Register controllers and Swagger services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.ConfigObject.AdditionalItems["tryItOutEnabled"] = true;
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/hello", () =>
{
    return "Hello, world!";
})
.WithName("GetHello");

app.MapDefaultEndpoints();

await app.RunAsync();
