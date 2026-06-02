using AutoWashPro.BLL.Services;
using AutoWashPro.DAL.Data;
using BLL.Helpers;
using BLL.Services;
using DAL.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PayOS;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// 1. SYSTEM CONFIGURATION & CONTROLLERS
// ==============================================================================
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
                message = errorMessage ?? "Dữ liệu đầu vào không hợp lệ.",
                details = (string?)null
            });
        };
    });

// ==============================================================================
// 2. CORS CONFIGURATION (Frontend Connection)
// ==============================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// ==============================================================================
// 3. DATABASE CONFIGURATION
// ==============================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AutoWashDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0))));

// ==============================================================================
// 4. AUTHENTICATION & SECURITY (JWT)
// ==============================================================================
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

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("AIChatPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString(),

            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));
});

// ==============================================================================
// 5. THIRD-PARTY & AI SERVICES (PayOS, PaddleOCR, LLM)
// ==============================================================================
// 5.1. PayOS Integration
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new PayOSClient(
        config["PayOS:ClientId"] ?? "",
        config["PayOS:ApiKey"] ?? "",
        config["PayOS:ChecksumKey"] ?? ""
    );
});

// 5.2. AI & OCR Models Integration
var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models/license_plate.onnx");
builder.Services.AddSingleton(new OnnxInferenceEngine(modelPath));

builder.Services.AddSingleton<PaddleOcrService>(sp =>
{
    var recModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models/rec_model.onnx");
    var dictPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models/dict.txt");
    var logger = sp.GetRequiredService<ILogger<PaddleOcrService>>();
    return new PaddleOcrService(recModelPath, dictPath, logger);
});

// ==============================================================================
// 6. DEPENDENCY INJECTION (BLL Services)
// ==============================================================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITierService, TierService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVehicleService, AutoWashPro.BLL.Services.VehicleService>();
builder.Services.AddScoped<IVehicleTypeService, AutoWashPro.BLL.Services.VehicleTypeService>();
builder.Services.AddScoped<IServiceService, AutoWashPro.BLL.Services.ServiceService>();
builder.Services.AddScoped<IBookingService, AutoWashPro.BLL.Services.BookingService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<IAIChatbotService, AIChatbotService>();
builder.Services.AddScoped<IAIModerationService, AIModerationService>();
builder.Services.AddHttpClient<ILLMService, GeminiAIService>();
builder.Services.AddScoped<IAIIntentService, AIIntentService>();
builder.Services.AddScoped<ILicensePlateService, LicensePlateService>();
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<IPhotoService, PhotoService>();
// ==============================================================================
// 7. BACKGROUND WORKERS
// ==============================================================================
builder.Services.AddScoped<IStaffManagementService, StaffManagementService>();

builder.Services.AddHostedService<AutoWashPro.API.Workers.AnnualTierResetWorker>();

// ==============================================================================
// 8. SWAGGER CONFIGURATION
// ==============================================================================
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

// ==============================================================================
// 9. BUILD APP & MIDDLEWARE PIPELINE
// ==============================================================================
var app = builder.Build();

app.UseMiddleware<AutoWashPro.API.Middlewares.ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

// ==============================================================================
// 10. DATABASE MIGRATION & SEEDING ON STARTUP
// ==============================================================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AutoWashDbContext>();

    context.Database.Migrate();
    SyncCustomerProfilePoints(context);

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

        var firstTier = context.Tiers.FirstOrDefault(t => t.MinAccumulatedPoints == 0)
            ?? new AutoWashPro.DAL.Entities.Tier
            {
                TierName = "Standard",
                PointMultiplier = 1.0,
                BookingWindowDays = 7,
                MinAccumulatedPoints = 0
            };

        if (firstTier.TierId == 0) context.Tiers.Add(firstTier);

        context.SaveChanges();

        context.CustomerProfiles.Add(new AutoWashPro.DAL.Entities.CustomerProfile
        {
            UserId = admin.UserId,
            FullName = "System Admin",
            TierId = firstTier.TierId,
            ChurnScore = 0,
            TotalPoint = 0,
            PromotionPoint = 0
        });

        context.SaveChanges();
    }
}

app.Run();

// ==============================================================================
// 11. LOCAL HELPER FUNCTIONS
// ==============================================================================
static void SyncCustomerProfilePoints(AutoWashDbContext context)
{
    const string completionPrefix = "Hoàn thành dịch vụ";
    var now = DateTime.UtcNow;

    var profiles = context.CustomerProfiles.ToList();
    var allLedgers = context.PointLedgers.ToList();
    var groupedLedgers = allLedgers.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.ToList());
    foreach (var profile in profiles)
    {
        if (!groupedLedgers.TryGetValue(profile.UserId, out var ledgers) || !ledgers.Any()) continue;

        var totalAdded = ledgers
            .Where(p => p.PointsAdded > 0 && (p.ExpiryDate == null || p.ExpiryDate > now))
            .Sum(p => p.PointsAdded);
        var totalDeducted = ledgers.Where(p => p.PointsDeducted > 0).Sum(p => p.PointsDeducted);
        var promotionFromLedger = ledgers
            .Where(p => p.PointsAdded > 0 && p.Reason.StartsWith(completionPrefix))
            .Sum(p => p.PointsAdded);

        if (profile.TotalPoint == 0 && profile.PromotionPoint == 0)
        {
            profile.TotalPoint = Math.Max(0, totalAdded - totalDeducted);
            profile.PromotionPoint = promotionFromLedger;
        }
    }

    context.SaveChanges();
}