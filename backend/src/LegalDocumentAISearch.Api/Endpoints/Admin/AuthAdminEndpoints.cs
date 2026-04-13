using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.Api.Endpoints.Admin;

public static class AuthAdminEndpoints
{
    public static RouteGroupBuilder MapAuthAdminEndpoints(this RouteGroupBuilder group)
    {
        group.MapIdentityApi<AdminUser>();

        return group;
    }
}
