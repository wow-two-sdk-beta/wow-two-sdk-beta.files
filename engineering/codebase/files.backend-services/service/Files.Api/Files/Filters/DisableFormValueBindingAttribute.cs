using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Files.Api.Files.Filters;

/// <summary>Keeps model binding from reading a request's form, so the action reads a multipart body as a stream.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DisableFormValueBindingAttribute : Attribute, IResourceFilter
{
    /// <inheritdoc />
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var factories = context.ValueProviderFactories;
        factories.RemoveType<FormValueProviderFactory>();
        factories.RemoveType<FormFileValueProviderFactory>();
        factories.RemoveType<JQueryFormValueProviderFactory>();
    }

    /// <inheritdoc />
    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
