using EventRegistration.Api.Behaviors;
using EventRegistration.Api.Database;
using EventRegistration.Api.Interfaces;
using EventRegistration.Api.Middlewares;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Serilog;

DotNetEnv.Env.Load();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Event Registration API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = new List<string>();

                foreach (var entry in context.ModelState)
                {
                    if (entry.Value is null || entry.Value.Errors.Count == 0)
                    {
                        continue;
                    }

                    var field = entry.Key.StartsWith("$.") ? entry.Key[2..] : entry.Key;

                    foreach (var error in entry.Value.Errors)
                    {
                        if (error.Exception is not null)
                        {
                            errors.Add($"'{field}' has an invalid value or the request body is not valid JSON.");
                        }
                        else if (string.IsNullOrWhiteSpace(error.ErrorMessage))
                        {
                            errors.Add($"'{field}' is not valid.");
                        }
                        else
                        {
                            errors.Add(error.ErrorMessage);
                        }
                    }
                }

                return new BadRequestObjectResult(new
                {
                    success = false,
                    timestamp = DateTime.UtcNow,
                    message = "One or more validation errors occurred.",
                    errors
                });
            };
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddSingleton<IEventRegistrationDatabase, EventRegistrationDatabase>();

    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("Frontend");
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}