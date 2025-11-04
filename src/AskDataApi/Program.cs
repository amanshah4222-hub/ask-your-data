using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using AskDataApi.Domain.Nl;
using AskDataApi.Domain.Query;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(o => o.AddPolicy("fe", p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB connection 
builder.Services.AddSingleton<Func<NpgsqlConnection>>(_ =>
{
    var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULT")
             ?? throw new InvalidOperationException("Missing DB connection");
    return () => new NpgsqlConnection(cs);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("NEON_AUTH_ISSUER");
        options.TokenValidationParameters.ValidateAudience = false; 
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
    });




builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AskData API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,       
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your access token here (no need to type 'Bearer ')."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddSingleton<SqlValidator>(_ => new SqlValidator(5000));
builder.Services.AddSingleton<QueryOrchestrator>(sp =>
{
    var connFactory = sp.GetRequiredService<Func<Npgsql.NpgsqlConnection>>();
    return new QueryOrchestrator(connFactory, TimeSpan.FromSeconds(15));
});

var app = builder.Build();
app.UseCors("fe");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.UseForwardedHeaders(new ForwardedHeadersOptions {
  ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

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
    Console.WriteLine("/me hit!");
    Console.WriteLine(JsonSerializer.Serialize(user).ToString());
    return Results.Ok(new
    {
        user_id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub")?.Value,
        email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirst("email")?.Value
    });
});
// .RequireAuthorization();

// Protected: ask about data
app.MapPost("/ask", async (AskRequest req, SqlValidator validator, QueryOrchestrator exec) =>
{
    if (string.IsNullOrWhiteSpace(req.Question))
        return Results.BadRequest(new { error = "QUESTION_REQUIRED" });

    var prompt = PromptBuilder.Build(req.Question, req.Limit);
    var validation = validator.Validate(prompt.Sql);

    if (!validation.IsValid)
    {
        return Results.BadRequest(new
        {
            error = "SQL_NOT_ALLOWED",
            details = validation.Errors
        });
    }

    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var (rows, elapsed) = await exec.ExecuteAsync(validation.RewrittenSql, prompt.Parameters, cts.Token);

        var explain = new
        {
            question = req.Question,
            generatedSql = prompt.Sql,
            rewrittenSql = validation.RewrittenSql,
            confidence = prompt.Confidence,
            notes = validation.Notes,
            elapsedMs = (int)elapsed.TotalMilliseconds,
            parameters = prompt.Parameters
        };

        return Results.Ok(new AskResponse(rows, explain));
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message);
    }
});


app.Run();
