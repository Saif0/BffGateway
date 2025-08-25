using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BffGateway.WebApi.Extensions;

public static class ActionResultExtensions
{
    public static ActionResult<TResponse> MapUpstreamStatusCode<TResponse>(this ControllerBase controller, TResponse body, int? upstreamStatusCode)
    {
        var status = upstreamStatusCode ?? 0;
        if (status == (int)HttpStatusCode.TooManyRequests)
            return controller.StatusCode((int)HttpStatusCode.TooManyRequests, body);
        if (status == (int)HttpStatusCode.RequestTimeout)
            return controller.StatusCode((int)HttpStatusCode.GatewayTimeout, body);
        if (status >= 500)
            return controller.StatusCode((int)HttpStatusCode.BadGateway, body);

        return controller.BadRequest(body);
    }
}


