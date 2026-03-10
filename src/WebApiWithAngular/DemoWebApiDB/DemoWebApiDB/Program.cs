using DemoWebApiDB.Services.Categories;
using DemoWebApiDB.Services.Products;
using Scalar.AspNetCore;
using Serilog;
using Microsoft.AspNetCore.Mvc.Formatters;
using DemoWebApiDB.Infrastructure.Middleware;
using DemoWebApiDB.Auth.Entities;
using Microsoft.AspNetCore.Identity;


var builder = WebApplication.CreateBuilder(args);

//---------- Add services to the container.

if( ! builder.Environment.IsEnvironment("Testing") )
{

    // 1.  Grab the connection string from the appsetting.json file
    const string ConnectionStringNAME = "DefaultConnectionString";
    const string MigrationsAssemblyNAME = "DemoWebApiDB";
    string connectionString
        = builder.Configuration.GetConnectionString(ConnectionStringNAME)
          ?? throw new InvalidOperationException($"Connection String '{ConnectionStringNAME}' not defined in appsettings file");

    // 2. Register the DataContext Service into the DI Container which uses the SQL Server
    builder.Services
        .AddDbContext<ApplicationDbContext>(options =>
        {
            // Register the SQL Server middleware.
            options.UseSqlServer(
                connectionString: connectionString,
                builderOptions => builderOptions.MigrationsAssembly(MigrationsAssemblyNAME));
        });

}


builder.Services
    .AddDataProtection();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    }
    )
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();



// 3. Register Controllers
//    Ensure Content Negotiation and Serialization Support for XML and JSON 
builder.Services
    .AddControllers(options =>
    {
        // Respect the Accept header sent by the browser/client
        options.RespectBrowserAcceptHeader = true;

        // Return 406 Not Acceptable if the client requests an unsupported format
        options.ReturnHttpNotAcceptable = true;

        // Enable support to define the Character-set in the Request "Accept" parameter
        var jsonOutputFormatter
             = options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().FirstOrDefault();
        jsonOutputFormatter?.SupportedMediaTypes.Add("application/json; charset=utf-8");

    })
    .AddJsonOptions(options =>
    {
        // Throw an exception if the JSON contains properties that are not in the model
        options.JsonSerializerOptions.UnmappedMemberHandling
            = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
    });
// NOTE: I am not enabling support for XML serialization since most productino APIs support JSON only.
//       return 406 "Not Acceptable" for unsupported formats, including XML.
// .AddXmlSerializerFormatters();               // Remove support for XML serialization




// 4. Register OpenAPI Support.  For more info: https://aka.ms/aspnet/openapi
builder.Services
    .AddOpenApi();


// 5. Register automatic model validation support with RFC7807 ProblemDetails response
builder.Services
    .Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory 
            = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Detail = "One or more validation errors occurred.",
                    Instance = context.HttpContext.Request.Path,
                    // TODO: Add TraceId and CoRelationId support to the ValidationProblemDetails model in future.
                };

                return new BadRequestObjectResult(problemDetails);
            };
    });


// 6. Register support for ProblemDetails for failed requests to the DI Container
builder.Services
    .AddProblemDetails();


// 7. Register application services to the DI Container

builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();


// 8. Register the Serilog Service
//    (a) Add the ScalarApiReference middleware to read OpenAPI documentation
//    (b) Add the CorrelationId middleware before adding the ScalarApiReference middleware
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();


// 9. Configure CORS Policy support needed for Angular Project
//    And enable the registered policy by adding the Middleware.
string? angularDevServer 
    = builder.Configuration.GetValue<string>("MyAppSettings:AngularDevServer");
string? angularCorsPolicyName
    = builder.Configuration.GetValue<string>("MyAppSettings:AngularCORSPolicyName");
if( !string.IsNullOrEmpty(angularDevServer ) 
    &&  !string.IsNullOrEmpty(angularCorsPolicyName ) )
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(angularCorsPolicyName, policy =>
        {
            policy
                .WithOrigins(angularDevServer)      // OR .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()                   // OR .WithMethods("GET", "PUT", "POST", "DELETE", "PATCH")
                .AllowCredentials();                // Needed for auth scenarios
        });
    });
}



var app = builder.Build();



//---------- Configure the HTTP request pipeline and Middleware

if (app.Environment.IsDevelopment())
{

    app.MapOpenApi();

    // Enable API Documentation middleware to work with OpenAPI
    app.MapScalarApiReference();


    // Add the CorrelationId middlware needed for Serilog
    app.Use(async (context, next) =>
    {
        var correlationId = context.TraceIdentifier;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next();
        }
    });


    // Add the registered CORS policy Middleware.
    // NOTE: must come before app.UseAuthorization() and app.MapControllers()
    if (!string.IsNullOrEmpty(angularDevServer)
        && !string.IsNullOrEmpty(angularCorsPolicyName))
    {
        app.UseCors(angularCorsPolicyName);
    }

}


// Register the CorrelationId middleware to ensure correlation IDs are included in logs and responses
// Ensure that it is registered before the Serilog Request Logging middleware to capture correlation IDs in logs.
app.UseMiddleware<CorrelationIdMiddleware>();


// Add Serilog Request Logging
// Ensure that it is registered before the Https Redirection and Authorization middleware
// to capture all requests in logs, including failed ones.
app.UseSerilogRequestLogging();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbcontext = services.GetRequiredService<ApplicationDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    await IdentitySeeder.SeedAsync(dbcontext, roleManager, userManager);
}

app.Run();
