namespace Moneytree.Account.Src.Config;

using StackExchange.Redis;

public class AppStore
{
    private EnvSchema _env { get; }
    private ConnectionMultiplexer RedisConn { get; set; }

    public AppStore(EnvSchema env)
    {
        _env = env;
        RedisConn = Connect();
    }

    private ConnectionMultiplexer Connect() =>
    ConnectionMultiplexer.Connect(_env.APP_STORE_URL);

    // public AppStore(EnvSchema env)
    // {
    //     _env = env;
    //     int attempts = 0;
    //     int sleepTime = 1;
    //     while (!AttemptConnection())
    //     {
    //         if (attempts > 3)
    //         {
    //             throw new Exception($"Attempts Exceeded Failed to connect to redis at {_env.APP_STORE_URL}");
    //         }
    //         attempts++;
    //         Console.WriteLine("Attempting to Connect To Redis");
    //         Thread.Sleep((sleepTime + attempts) * 2);
    //     }

    // }

    private bool AttemptConnection()
    {
        if (RedisConn != null && RedisConn.IsConnected)
        {
            return true;
        }

        if (RedisConn != null && !RedisConn.IsConnected)
        {
            RedisConn.Dispose();
        }
        RedisConn = Connect();

        bool isConnected = RedisConn.IsConnected;

        return isConnected;
    }

    public IDatabase GetDatabase()
    {
        AttemptConnection();
        return RedisConn.GetDatabase();
    }

    public String GenerateKey(String? prefix)
    {
        var key = new Guid();

        if (prefix == null)
        {
            return key.ToString();
        }
        else
        {
            return $"{prefix}:{key}";
        }
    }
}