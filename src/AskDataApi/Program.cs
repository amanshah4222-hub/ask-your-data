using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Npgsql;
using AskDataApi.Domain.Nl;
using AskDataApi.Domain.Query;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using AskDataApi.Helpers;
using AskDataApi.Services;

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

builder.Services.AddSingleton<ISchemaService, SchemaService>();
builder.Services.AddHttpClient<OpenAiSqlService>();

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
app.MapPost("/ask", async (
    AskRequest req,
    SqlValidator validator,
    QueryOrchestrator exec,
 Func<NpgsqlConnection> connFactory,
    ClaimsPrincipal user,
    OpenAiSqlService llm) =>
{
    if (string.IsNullOrWhiteSpace(req.Question))
        return Results.BadRequest(new { error = "QUESTION_REQUIRED" });

    var (rawSql, conf) = await llm.BuildSqlAsync(req.Question, req.Limit);

    var validation = validator.Validate(rawSql);
    if (!validation.IsValid)
    {
        await AuditHelper.LogAuditAsync(connFactory, req.Question, rawSql, "", conf, 0, user, validation.Notes, "SQL_NOT_ALLOWED");
        return Results.BadRequest(new
        {
            error = "SQL_NOT_ALLOWED",
            details = validation.Errors,
            llmSql = rawSql
        });

       
    }
    try
    {
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
    var (rows, elapsed) = await exec.ExecuteAsync(validation.RewrittenSql, null, cts.Token);

    var explain = new
    {
        question = req.Question,
        generatedSql = rawSql,
        rewrittenSql = validation.RewrittenSql,
        confidence = conf,
        notes = validation.Notes,
        elapsedMs = (int)elapsed.TotalMilliseconds,
    };

     await AuditHelper.LogAuditAsync(
            connFactory,
            req.Question,
            rawSql,
            validation.RewrittenSql,
            conf,
            (int)elapsed.TotalMilliseconds,
            user,
            validation.Notes,
            null);

    return Results.Ok(new AskResponse(rows, explain));
    }
    catch (Exception ex)
    {
        await AuditHelper.LogAuditAsync(connFactory, req.Question, rawSql, validation.RewrittenSql,
            conf, 0, user, validation.Notes, ex.Message);
        return Results.Problem(detail: ex.Message);
    }
});

// Audit endpoint
app.MapGet("/audit", async (Func<NpgsqlConnection> connFactory) =>
{
    await using var conn = connFactory();
    var rows = await conn.QueryAsync(@"
        select id, asked_at, question, user_email, confidence, elapsed_ms
        from ask_audit
        order by asked_at desc
        limit 50;
    ");
    return Results.Ok(rows);
}); 


app.Run();
