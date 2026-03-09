var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// Register the CORS POLICY
const string CORSPolicyName = "AllowXhrDemo";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CORSPolicyName,
        policy =>
        {
            policy
                .AllowAnyOrigin()   // allow any origin for demo purposes
                                    // OR .WithOrigins('<url>')
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable the CORS Policy Middleware
app.UseCors(CORSPolicyName);

app.UseAuthorization();

app.MapControllers();

app.Run();
