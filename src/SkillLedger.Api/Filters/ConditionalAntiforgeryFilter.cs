using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SkillLedger.Api.Filters;

/// <summary>
/// A filter that conditionally validates antiforgery tokens based on the environment.
/// In testing environments, antiforgery validation is bypassed to simplify test setup.
/// </summary>
public class ConditionalAntiforgeryFilter : IAsyncActionFilter
{
    private readonly IAntiforgery _antiforgery;
    private readonly IWebHostEnvironment _environment;

    public ConditionalAntiforgeryFilter(IAntiforgery antiforgery, IWebHostEnvironment environment)
    {
        _antiforgery = antiforgery;
        _environment = environment;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Skip antiforgery validation in testing environment
        if (_environment.IsEnvironment("Testing"))
        {
            await next();
            return;
        }

        // Validate antiforgery token in production environments
        try
        {
            await _antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new { message = "Invalid antiforgery token." });
            return;
        }

        await next();
    }
}