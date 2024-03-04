using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace ReverseProxyPoC;

internal class CustomerProfileTransformFactory : ITransformFactory
{
    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        context.AddRequestTransform(transformContext =>
        {
            if (transformContext.HttpContext.Request.Query?.TryGetValue("customerId", out StringValues customerId) == true)
            {
                transformContext.ProxyRequest.RequestUri = new(new(transformContext.DestinationPrefix), $"/api/customers/{customerId}/language");
            }
            return default;
        });

        return true;
    }

    public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue("CustomerProfileTransform", out var value))
        {
            if (string.IsNullOrEmpty(value))
            {
                context.Errors.Add(new ArgumentException("A boolean value for CustomerProfileTransform is required"));
            }

            return true; // Matched
        }
        return false;
    }
}

internal class CustomerProfileTransformProvider : ITransformProvider
{
    public void Apply(TransformBuilderContext context)
    {
        if (context.Route.Transforms?.Any(x => x.ContainsKey("CustomerProfileTransform")) == true
            && string.Equals(context.Route.ClusterId, "customerProfileService", StringComparison.OrdinalIgnoreCase))
        {
            context.AddRequestTransform(transformContext =>
            {
                if (transformContext.HttpContext.Request.Query?.TryGetValue("customerId", out StringValues customerId) == true)
                {
                    transformContext.ProxyRequest.RequestUri = new(new(transformContext.DestinationPrefix), $"/api/customers/{customerId}/language");
                }
                return default;
            });
        }
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }
}
