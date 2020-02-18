using Amazon.XRay.Recorder.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AWS.Beanstalk.WebApplication.ActionFilters
{
    public class XRayActionFilter : Attribute, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {

        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            AWSXRayRecorder.Instance.AddMetadata("RequestCurrentMethod", MethodBase.GetCurrentMethod());
            AWSXRayRecorder.Instance.AddMetadata("RequestPath", context.HttpContext.Request.Path);
        }
    }
}

