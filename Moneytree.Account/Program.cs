using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpLogging;
using Moneytree.Account.Src.Config;
using Moneytree.Account.Src.Internal.Filters;
using Moneytree.Account.Src.Utils;

using Newtonsoft.Json.Serialization;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Moneytree.Account.Src.Http.Middlewares;

var builder = WebApplication.CreateBuilder(args);
Env.Load();
EnvSchema env = new();

{
    builder.Services.AddSingleton<EnvSchema>();
    builder.Services.AddSingleton<AppStore>();
    builder.Services.AddSingleton<Helpers>();
}

{
    builder.Services.AddDbContext<Db>((sp, options) =>
    {
        options.UseNpgsql(env.DB_PATH).UseSnakeCaseNamingConvention();
    });

    var Jwtkey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(env.JWT_SECRET));
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = env.JWT_ISSUER,
            ValidAudience = env.JWT_AUDIENCE,
            IssuerSigningKey = Jwtkey
        };
    });
    builder.Services.AddAuthorization();
    builder.Services.AddControllers();
    builder.Services.AddMvc().AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };
    });

    builder.Services.AddOpenApi(c =>
    {
        c.AddSchemaTransformer<OpenApiSnakeCaseSchemaFilter>();
    });

    builder.Services.AddHttpLogging(options => // <--- Setup logging
    {
        // Specify all that you need here:
        options.LoggingFields = HttpLoggingFields.RequestHeaders |
                                HttpLoggingFields.RequestBody |
                                HttpLoggingFields.ResponseHeaders |
                                HttpLoggingFields.ResponseBody;
    });
    builder.Logging.AddSimpleConsole(c => c.SingleLine = true);
}


var app = builder.Build();

await using var scope = app.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<Db>();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Moneytree Accounts API");
    });
    await db.Database.EnsureDeletedAsync();  // WARNING: wipes the DB
    await db.Database.EnsureCreatedAsync();
}
else
{
    db.Database.Migrate();  // Applies pending migrations
}
{
    // confirm connection works
    var canConnect = await db.Database.CanConnectAsync();
    app.Logger.LogInformation("Can connect to database: {CanConnect}", canConnect);
    app.UseAuthentication();
    app.UseMiddleware<GetUserFromClaimsMiddleware>();
    app.UseAuthorization();
    app.UseExceptionHandler("/error");
    app.UseHttpLogging();
    app.UseCors();
    app.UseHttpsRedirection();
    app.MapControllers();
}
app.Run();

