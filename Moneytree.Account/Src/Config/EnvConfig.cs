namespace Moneytree.Account.Src.Config;

public class EnvSchema
{
    public string SERVICE_NAME { get; }
    public string SERVICE_ENV { get; }
    public string DB_HOST { get; }
    public int DB_PORT { get; }
    public string DB_USERNAME { get; }
    public string DB_PASSWORD { get; }
    public string DB_NAME { get; }

    public string DB_PATH { get; }

    public string APP_STORE_URL { get; }

    public string JWT_SECRET { get; }

    public int JWT_EXPIRY { get; }

    public string JWT_ISSUER { get; }

    public string JWT_AUDIENCE { get; }

    public EnvSchema()
    {

        SERVICE_NAME = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? throw new Exception("SERVICE_NAME not found.");

        SERVICE_ENV = Environment.GetEnvironmentVariable("SERVICE_ENV") ?? throw new Exception("SERVICE_ENV not found.");

        DB_HOST = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new Exception("DB_HOST not found.");

        DB_PORT = (int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var val) ? val : (int?)null) ?? throw new Exception("DB_PORT not found.");

        DB_PASSWORD = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new Exception("DB_PASSWORD not found.");

        DB_USERNAME = Environment.GetEnvironmentVariable("DB_USERNAME") ?? throw new Exception("DB_USERNAME not found.");

        DB_NAME = Environment.GetEnvironmentVariable("DB_NAME") ?? throw new Exception("DB_NAME not found.");

        DB_PATH = $"Host={DB_HOST};Port={DB_PORT};Username={DB_USERNAME};Password={DB_PASSWORD};Database={DB_NAME}";

        APP_STORE_URL = Environment.GetEnvironmentVariable("APP_STORE_URL") ?? throw new Exception("APP_STORE_URL not found.");

        JWT_SECRET = Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new Exception("JWT_SECRET not found.");

        JWT_EXPIRY = (int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY"), out var jwt_val) ? jwt_val : (int?)null) ?? throw new Exception("JWT_EXPIRY not found.");

        JWT_ISSUER = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new Exception("JWT_ISSUER not found.");

        JWT_AUDIENCE = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new Exception("JWT_AUDIENCE not found.");
    }
}