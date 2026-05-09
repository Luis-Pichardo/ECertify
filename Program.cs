using eCertify.Interfaces;
using eCertify.Services;
using eCertify.Services.Front;
using eCertify.Settings;
using Microsoft.EntityFrameworkCore;
using eCertify.Data;
using System.Text.Json.Serialization;
using eCertify.Utils;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Text.Json;
using System.Net.Http.Headers;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.License.SetNonCommercialPersonal("Developer");

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAnyOrigin", policy => {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

builder.Services.AddDbContext<SogeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaConexion")));

// API Services
builder.Services.AddSingleton<QuerySqlService>();
builder.Services.AddScoped<IFacturasElectronicasService, FacturasElectronicasService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IFacturasXmlService, FacturasXmlService>();
builder.Services.AddScoped<ISemillaService, SemillaService>();
builder.Services.AddScoped<ICommercialApprovalService, CommercialApprovalService>();
builder.Services.AddScoped<IRecepcionEcfService, RecepcionEcfService>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.AddScoped<IEmpresaService, EmpresaService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFileStorageManager, FileStorageManager>();
builder.Services.AddScoped<IResumenesXmlService, ResumenesXmlService>();
builder.Services.AddScoped<IHistorialPruebasExcelService, HistorialPruebasExcelService>();
builder.Services.AddScoped<IHistorialPagoService, HistorialPagoService>();

// Front Services
builder.Services.AddSingleton<TokenAuthService>();
builder.Services.AddTransient<eCertify.Utils.TokenHandler>();
builder.Services.AddScoped<IPasosCompletadosService, PasosCompletadosService>();
builder.Services.AddScoped<UserInfoService>();
builder.Services.AddScoped<PlanValidator>();

// HttpClients
var apiUrl    = builder.Configuration["ApiUrl"]!;
var apiUrlProd = builder.Configuration["ApiUrlProd"]!;
var baseUrl   = builder.Configuration["ApiSettings:BaseUrl"];

builder.Services.AddHttpClient<AuthService>(client => {
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient<EmpresaAuthService>(client => {
    client.BaseAddress = new Uri(apiUrlProd);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("ApiClient", client => {
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddHttpMessageHandler<eCertify.Utils.TokenHandler>();

builder.Services.AddHttpClient("ApiClientProd", client => {
    client.BaseAddress = new Uri(apiUrlProd);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("RncApiClient", client => {
    client.BaseAddress = new Uri("https://rnc.megaplus.com.do");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = "UsuarioScheme";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents {
        OnMessageReceived = context => {
            Console.WriteLine($"[JWT] Authorization: {context.Request.Headers["Authorization"]}");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context => {
            Console.WriteLine($"[JWT] Auth error: {context.Exception}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context => {
            Console.WriteLine($"[JWT] Token válido para EmpresaID: {context.Principal?.FindFirst("EmpresaID")?.Value}");
            return Task.CompletedTask;
        },
        OnChallenge = context => {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(new {
                error = "Unauthorized",
                message = "El token es inválido o no fue provisto."
            }));
        }
    };
})
.AddCookie("UsuarioScheme", options => {
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.Cookie.Name = "UsuarioAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(120);
    options.SlidingExpiration = true;
})
.AddCookie("EmpresaScheme", options => {
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.Cookie.Name = "EmpresaAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(120);
    options.SlidingExpiration = true;
});

builder.Services.AddSession(options => {
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddProvider(new FileLoggerProvider("logs/eCertify.log"));
builder.Services.AddLogging();

// Swagger (solo en Development)
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo {
        Title = "eCertify API - Emisor electrónico e-CF",
        Version = "v1"
    });
    if (!string.IsNullOrEmpty(baseUrl))
        options.AddServer(new OpenApiServer { Url = baseUrl });
    options.OperationFilter<FileUploadOperationFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT como: **Bearer {token}**"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "Bearer",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            Array.Empty<string>()
        }
    });
    options.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAnyOrigin");
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapControllers();
app.MapRazorPages();

app.Run();
