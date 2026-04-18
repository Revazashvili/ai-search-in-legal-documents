namespace LegalDocumentAISearch.Api.Endpoints.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin");

        group.MapAuthAdminEndpoints();
        group.MapDocumentAdminEndpoints();

        return app;
    }
}
