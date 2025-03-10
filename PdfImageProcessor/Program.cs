using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Kestrel to allow large files
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024 * 1024 * 100; // 100MB
});

// ✅ Configure FormOptions for file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024 * 1024 * 100; // 100MB
});

// ✅ Configure IIS settings (if using IIS)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 1024 * 1024 * 100; // 100MB
});

// ✅ Add MVC and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


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

var app = builder.Build();

// ✅ Place the Middleware **before** `app.UseRouting()`
app.Use((context, next) =>
{
    var maxSizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (maxSizeFeature != null)
    {
        maxSizeFeature.MaxRequestBodySize = 1024 * 1024 * 100; // 100MB
    }
    return next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ✅ Map routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
