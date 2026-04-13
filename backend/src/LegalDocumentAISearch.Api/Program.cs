using LegalDocumentAISearch.Api.Endpoints.Admin;
using LegalDocumentAISearch.Api.Endpoints.User;
using LegalDocumentAISearch.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
var allowedOrigins = builder.Configuration.GetRequiredSection("Cors:AllowedOrigins").Get<string[]>()!;

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapAdminEndpoints();
app.MapUserEndpoints();

app.Run();
