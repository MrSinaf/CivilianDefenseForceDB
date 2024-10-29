using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataBaseCDF.Services;

public class AdminAuthorize : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            if (!user.IsAdmin())
                context.Result = new ForbidResult();
        }
        else
            context.Result = new ChallengeResult();
    }
}