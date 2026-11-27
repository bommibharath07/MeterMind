using Npgsql;
namespace MeterMind.Integration.Tests;
public class SqlArtifactTests {
    [DatabaseFact] public async Task IdempotentSqlAppliesToFreshAndExistingSchema(){
        var config=new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("METERMIND_TEST_CONNECTION"));
        if(!config.Database!.EndsWith("_test"))throw new InvalidOperationException("SQL artifact test requires an isolated _test database.");
        var schema="artifact_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(config.ConnectionString);await admin.OpenAsync();
        await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try{
            config.SearchPath=schema;await using var connection=new NpgsqlConnection(config.ConnectionString);await connection.OpenAsync();
            var script=await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"migrations.sql"));
            for(int pass=0;pass<2;pass++){await using var command=new NpgsqlCommand(script,connection){CommandTimeout=120};await command.ExecuteNonQueryAsync();}
            await using var count=new NpgsqlCommand("SELECT count(*) FROM \"__EFMigrationsHistory\"",connection);Assert.Equal(2L,await count.ExecuteScalarAsync());
        }finally{
            // This schema name is a local UUID generated above, never an external database/schema input.
            await using var drop=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await drop.ExecuteNonQueryAsync();
        }
    }
}
