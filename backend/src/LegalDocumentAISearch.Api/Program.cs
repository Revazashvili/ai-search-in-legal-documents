using LegalDocumentAISearch.Api.Endpoints.Admin;
using LegalDocumentAISearch.Api.Endpoints.User;
using LegalDocumentAISearch.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapAdminEndpoints();
app.MapUserEndpoints();

app.Run();
