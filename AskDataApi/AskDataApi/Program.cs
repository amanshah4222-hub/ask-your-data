using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(o => o.AddPolicy("fe", p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB connection (Neon)
builder.Services.AddSingleton<Func<NpgsqlConnection>>(_ =>
{
    var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULT")
             ?? builder.Configuration.GetConnectionString("Default")
             ?? throw new InvalidOperationException("Missing DB connection");
    return () => new NpgsqlConnection(cs);
});

// JWT (Neon Auth)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("NEON_AUTH_ISSUER");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidateAudience = false
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Headers["x-stack-access-token"].ToString();
                if (string.IsNullOrWhiteSpace(token) && 
                    ctx.Request.Headers.Authorization.ToString().StartsWith("Bearer"))
                {
                    token = ctx.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                }
                ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseCors("fe");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

// Health
app.MapGet("/healthz", () => Results.Ok(new { ok = true, service = "AskDataApi", ts = DateTimeOffset.UtcNow }));

// Public endpoint: schema
app.MapGet("/schema", async (Func<NpgsqlConnection> connFactory) =>
{
    Console.WriteLine("Schema api hit");
    await using var conn = connFactory();
    var rows = await conn.QueryAsync(@"
        select table_schema, table_name
        from information_schema.tables
        where table_schema not in ('pg_catalog','information_schema')
        order by table_schema, table_name;");
    return Results.Ok(rows);
});

// Protected: authenticated user info
app.MapGet("/me", (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        user_id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub")?.Value,
        email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirst("email")?.Value
    });
}).RequireAuthorization();

app.Run();
