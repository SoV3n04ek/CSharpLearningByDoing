using WebApiToTestDependencyInjectionAndLifeCyclesLogger;

/* TASK Requirements 
 * Task: “DI & LifeCycles Logger”

Create a new ASP.NET Core Web API project.

Create three interfaces: ITransientService, IScopedService, ISingletonService.

Implement them in a single LifetimeService class that generates a new Guid in the constructor.

Register them in Program.cs with the appropriate lifecycles.

Inject all three interfaces into the Controller and into another helper class, MiddleService.

Output all GUIDs in the API response.

Question: Why are the GUIDs of the Scoped service in the controller and in
MiddleService the same for a single request, but the Transient ones are different?
*/

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddTransient<ITransientService, LifeTimeService>();
builder.Services.AddScoped<IScopedService, LifeTimeService>();
builder.Services.AddSingleton<ISingletonService, LifeTimeService>();
builder.Services.AddTransient<MiddleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();