using System;
using System.Text.Json;
using StackExchange.Redis;
using Npgsql;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Worker started...");
        
        // Connect to Redis with retry
        ConnectionMultiplexer redis = null;
        int redisRetryCount = 0;
        while (redis == null && redisRetryCount < 10)
        {
            try
            {
                redis = ConnectionMultiplexer.Connect("redis:6379");
                Console.WriteLine("Connected to Redis");
            }
            catch (Exception ex)
            {
                redisRetryCount++;
                Console.WriteLine($"Redis connection attempt {redisRetryCount} failed: {ex.Message}");
                System.Threading.Thread.Sleep(5000);
            }
        }
        
        if (redis == null)
        {
            Console.WriteLine("Failed to connect to Redis after 10 attempts. Exiting.");
            return;
        }
        
        var db = redis.GetDatabase();
        
        var pgConnectionString = "Host=postgres;Username=uservote;Password=userpass;Database=votedb";
        
        // Connect to PostgreSQL with retry
        NpgsqlConnection pgConn = null;
        int pgRetryCount = 0;
        while (pgConn == null && pgRetryCount < 10)
        {
            try
            {
                pgConn = new NpgsqlConnection(pgConnectionString);
                pgConn.Open();
                Console.WriteLine("Connected to PostgreSQL");
            }
            catch (Exception ex)
            {
                pgRetryCount++;
                Console.WriteLine($"PostgreSQL connection attempt {pgRetryCount} failed: {ex.Message}");
                System.Threading.Thread.Sleep(5000);
            }
        }
        
        if (pgConn == null)
        {
            Console.WriteLine("Failed to connect to PostgreSQL after 10 attempts. Exiting.");
            return;
        }
        
        // Create table if not exists
        using var cmd = new NpgsqlCommand(
            "CREATE TABLE IF NOT EXISTS votes (id SERIAL PRIMARY KEY, vote VARCHAR(10) NOT NULL, created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)",
            pgConn
        );
        cmd.ExecuteNonQuery();
        Console.WriteLine("Created/verified votes table");
        
        while (true)
        {
            try
            {
                var voteData = db.ListRightPop("votes");
                
                if (!voteData.IsNullOrEmpty)
                {
                    string voteJsonString = voteData.ToString();
                    
                    if (!string.IsNullOrEmpty(voteJsonString))
                    {
                        var voteJson = JsonDocument.Parse(voteJsonString);
                        var vote = voteJson.RootElement.GetProperty("vote").GetString();
                        
                        using var insertCmd = new NpgsqlCommand(
                            "INSERT INTO votes (vote) VALUES (@vote)",
                            pgConn
                        );
                        insertCmd.Parameters.AddWithValue("vote", vote);
                        insertCmd.ExecuteNonQuery();
                        
                        Console.WriteLine($"Processed vote for: {vote}");
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing vote: {ex.Message}");
                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
