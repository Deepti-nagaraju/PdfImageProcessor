using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using PdfImageProcessor.Services;
using System.Text;
using Microsoft.EntityFrameworkCore;

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
/**
JWT token authentication - validating the JWT token that came from the request
**/
// Getting the exact TokenKey which was used to create JWT
string? tokenKeyString = builder.Configuration.GetSection("AuthSettings:TokenKey").Value;
// Creating the actual key from TokenKey
SymmetricSecurityKey tokenKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(
        tokenKeyString != null ? tokenKeyString : ""
    )
);
// Parameters to tell how to Validate the token
TokenValidationParameters tokenValidationParameters = new TokenValidationParameters()
{
    IssuerSigningKey = tokenKey,
    ValidateIssuerSigningKey = true,
    ValidateIssuer = false,
    ValidateAudience = false
};
// Use JwtBearer Authentication scheme to validate token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = tokenValidationParameters;
    });

/**
End of JWT token authentication
**/

builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
var app = builder.Build();
app.UseCors("AllowAllOrigins");

// Configure the HTTP request pipeline.

app.UseSwagger();
    app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
