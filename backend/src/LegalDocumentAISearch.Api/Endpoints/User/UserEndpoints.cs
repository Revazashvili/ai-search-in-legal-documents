namespace LegalDocumentAISearch.Api.Endpoints.User;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapSearchEndpoints();

        return app;
    }
}
