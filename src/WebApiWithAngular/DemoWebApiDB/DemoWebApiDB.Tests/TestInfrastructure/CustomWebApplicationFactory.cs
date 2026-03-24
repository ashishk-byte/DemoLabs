using System.Data.Common;

using DemoWebApiDB.Data.Data;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace DemoWebApiDB.Tests.TestInfrastructure;


/// <summary>
///     CustomWebApplicationFactory is used for integration testing.
///     It replaces SQL Server with SQLite In-memory Database,
///     applies migrations and seeds deterministic test data.
/// </summary>
/// <remarks>
///     The factory will:
///     - create new SQLite connection per test
///     - ensure that the connection is kept open for the lifetime of the test 
///     - apply migrations
///     - seed test data
///     
///  VERY IMPORTANT NOTE:
///     WebApplicationFactory spins up the real API inside the test runner.
///     It uses the "testhost.deps.json" file to inform:
///     - what assemblies exist
///     - what dependencies to load
///     - how to boot the app.
///     SOLUTION: 
///     (a) Configure the API Project to preserve compilation metadata, by adding the following in .CSPROJ file:
///             <PreserveCompilationContext>true</PreserveCompilationContext>
///     (b) Add Project Reference to API Project, in the Test Project.
/// </remarks>
public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{

    private DbConnection? _connection;


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ----- 1. Set the Environment Variable so that SQL Server will not be loaded in Program.cs of the API Project

        builder.UseEnvironment("Testing");   // 🔥 CRITICAL LINE

        builder.ConfigureServices(services =>
        {
            // ----- 2. Create SQLite in-memory connection
            //          Ensure that each instance of the Database created for the test-run has a unique name!
            var dbName = $"TestDB_{Guid.NewGuid()}";
            var connectionString = $"DataSource={dbName};Mode=memory;Cache=Shared";

            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // ----- 3. Build provider + migrate + seed

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureDeleted();        // in case cached db exists, delete it.  Possible due to race conditions.
            db.Database.EnsureCreated();        // 
            db.Database.Migrate();              // ensure Schema is created for SQLite and apply all migrations

            TestDatabaseSeeder.Seed(db);        // seed the test data
        });
    }


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }

}