using AiBotOrderingSystem.Data;
using AiBotOrderingSystem.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// SERVICES
// =========================================================

// MVC
builder.Services.AddControllersWithViews();

// DbContext: PostgreSQL
builder.Services.AddDbContext<AiBotOrderingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// SignalR
builder.Services.AddSignalR();

// SESSION (added)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// OPTIONAL CORS FOR DEV
// builder.Services.AddCors(options =>
// {
//     options.AddDefaultPolicy(policy =>
//         policy.AllowAnyHeader()
//               .AllowAnyMethod()
//               .AllowCredentials()
//               .SetIsOriginAllowed(_ => true));
// });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// builder.Services.AddSwaggerGen(c =>
// {
//     // Define the API Key scheme
//     c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//     {
//         Description = "Enter API Key. Example: X-API-KEY: mysecretkey",
//         Name = "X-API-KEY",
//         In = Microsoft.OpenApi.Models.ParameterLocation.Header,
//         Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
//         Scheme = "ApiKey"
//     });

//     // Apply API Key scheme to all operations
//     c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
//     {
//         {
//             new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//             {
//                 Reference = new Microsoft.OpenApi.Models.OpenApiReference
//                 {
//                     Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
//                     Id = "ApiKey"
//                 }
//             },
//             new string[] {}
//         }
//     });
// });


var app = builder.Build();

// =========================================================
// MIDDLEWARE PIPELINE
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

    app.UseSwagger();
    app.UseSwaggerUI();

app.UseHttpsRedirection();

// static files
app.UseStaticFiles();
app.MapStaticAssets();

app.UseRouting();

// SESSION must be BEFORE endpoints
app.UseSession();

// CORS (if enabled)
// app.UseCors();

app.UseAuthorization();

// =========================================================
// ENDPOINTS
// =========================================================

// MVC Controllers
app.MapControllers();

// SignalR Hub
app.MapHub<OrderHub>("/orderHub");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

app.Run();
