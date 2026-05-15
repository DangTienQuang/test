using AutoWashPro.BLL.Services;
using AutoWashPro.DAL.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errorMessage = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault();

            return new BadRequestObjectResult(new
            {
                statusCode = 400,
                message = errorMessage ?? "Dữ liệu đầu vào không hợp lệ."
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Chỉ cần dán Token (JWT) vào đây. Hệ thống sẽ tự động thêm 'Bearer ' đằng trước.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AutoWashDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITierService, TierService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVehicleService, AutoWashPro.BLL.Services.VehicleService>();
builder.Services.AddScoped<IVehicleTypeService, AutoWashPro.BLL.Services.VehicleTypeService>();
builder.Services.AddScoped<IServiceService, AutoWashPro.BLL.Services.ServiceService>();

var app = builder.Build();
app.UseMiddleware<AutoWashPro.API.Middlewares.ExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AutoWashDbContext>();

    context.Database.EnsureCreated();

    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        var admin = new AutoWashPro.DAL.Entities.User
        {
            PhoneNumber = "0999999999",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            Status = "Active"
        };
        context.Users.Add(admin);

        var firstTier = context.Tiers.FirstOrDefault(t => t.MinAccumulatedPoints == 0);
        if (firstTier == null)
        {
            firstTier = new AutoWashPro.DAL.Entities.Tier
            {
                TierName = "Standard",
                PointMultiplier = 1.0,
                BookingWindowDays = 7,
                MinAccumulatedPoints = 0
            };
            context.Tiers.Add(firstTier);
            context.SaveChanges();
        }

        context.CustomerProfiles.Add(new AutoWashPro.DAL.Entities.CustomerProfile
        {
            UserId = admin.UserId,
            FullName = "System Admin",
            TierId = firstTier.TierId,
            ChurnScore = 0
        });

        context.Wallets.Add(new AutoWashPro.DAL.Entities.Wallet
        {
            UserId = admin.UserId,
            MainBalance = 0,
            TotalLoyaltyPoints = 0
        });

        context.SaveChanges();
    }
}

app.Run();