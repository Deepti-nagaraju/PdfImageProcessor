using PdfImageProcessor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<PdfProcessingService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy => policy.AllowAnyOrigin() // Allows all origins
                        .AllowAnyMethod() // Allows GET, POST, PUT, DELETE
                        .AllowAnyHeader()); // Allows all headers
});

var app = builder.Build();
app.UseCors("AllowAllOrigins");

// Configure the HTTP request pipeline.

app.UseSwagger();
    app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
