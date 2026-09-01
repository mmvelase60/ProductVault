using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using ProductVault.Data;
using ProductVault.Models;
using ProductVault.Services;
using System.Text;

namespace ProductVault;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)), mysql => mysql.EnableRetryOnFailure()));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key is not configured. Set Jwt:Key with User Secrets.");
        var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
        var jwtAudience = builder.Configuration["Jwt:Audience"]!;
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });
        builder.Services.AddAuthorization();
        var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options => options.AddPolicy("angular", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // Angular selects another local port when its preferred port is occupied.
                policy.SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttp
                    && uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                policy.WithOrigins(configuredCorsOrigins);
            }

            policy.AllowAnyHeader().AllowAnyMethod();
        }));
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IProductCodeGenerator, ProductCodeGenerator>();
        builder.Services.AddScoped<IExcelProductService, ExcelProductService>();
        builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
        builder.Services.AddScoped<IApplicationEmailSender, SmtpEmailSender>();
        builder.Services.AddScoped<IEmailVerificationCodeService, EmailVerificationCodeService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseCors("angular");
        app.UseHttpMetrics(options => options.ReduceStatusCodeCardinality());

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");
        if (app.Environment.IsDevelopment())
        {
            app.MapMetrics("/metrics");
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.Run();
    }
}
