using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT Settings from configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

// Add Authentication with JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // For demo purposes - In production, use a real authority/identity provider
        options.RequireHttpsMetadata = jwtSettings.GetValue<bool>("RequireHttpsMetadata");
        
        // Token validation parameters
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Issuer validation
            ValidateIssuer = jwtSettings.GetValue<bool>("ValidateIssuer"),
            ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
            
            // Audience validation
            ValidateAudience = jwtSettings.GetValue<bool>("ValidateAudience"),
            ValidAudience = jwtSettings.GetValue<string>("Audience"),
            
            // Lifetime validation
            ValidateLifetime = jwtSettings.GetValue<bool>("ValidateLifetime"),
            ClockSkew = TimeSpan.FromMinutes(5),
            
            // Signing key validation
            ValidateIssuerSigningKey = true,
            // For demo: Using symmetric key. In production, use asymmetric keys from your auth server
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("your-super-secret-key-min-32-chars-long-for-security")
            )
        };
        
        // Event handlers for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });

// Add Authorization with policies
builder.Services.AddAuthorization(options =>
{
    // Policy: Require Admin role
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
    
    // Policy: Require User or Admin role
    options.AddPolicy("UserPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("User", "Admin");
    });
    
    // Policy: Require specific scope
    options.AddPolicy("RequireApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "api.read", "api.write");
    });
    
    // Policy: Require specific permission claim
    options.AddPolicy("RequireReadPermission", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("permissions", "read");
    });
    
    // Policy: Combine multiple requirements
    options.AddPolicy("AdvancedPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
        policy.RequireClaim("scope", "api.write");
        policy.RequireClaim("permissions", "write", "delete");
    });
});

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Middleware pipeline - ORDER MATTERS!
app.UseAuthentication();
app.UseAuthorization();

// Test endpoint to generate JWT token for testing
app.MapGet("/auth/token", (string role = "User", string scope = "api.read") =>
{
    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes("your-super-secret-key-min-32-chars-long-for-security");
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim("sub", "test-user-123"),
            new System.Security.Claims.Claim("name", "Test User"),
            new System.Security.Claims.Claim("role", role),
            new System.Security.Claims.Claim("scope", scope),
            new System.Security.Claims.Claim("permissions", "read"),
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = jwtSettings.GetValue<string>("Issuer"),
        Audience = jwtSettings.GetValue<string>("Audience"),
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature
        )
    };
    
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);
    
    return Results.Ok(new
    {
        token = tokenString,
        expires = tokenDescriptor.Expires,
        role = role,
        scope = scope,
        usage = "Use: Authorization: Bearer <token>"
    });
});

// Map reverse proxy routes with authentication/authorization
app.MapReverseProxy();

app.Run();
