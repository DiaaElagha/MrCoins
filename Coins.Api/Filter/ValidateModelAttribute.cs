using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Coins.Api.Filter
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //Check model state in one place
            if (!context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // check exption in one place.
            //if (!context.ExceptionHandled && context.Exception != null)
            //{
            //    context.Result = new ObjectResult("Sorry! an error occurred, please call technical support.")
            //    {
            //        StatusCode = (int)HttpStatusCode.InternalServerError,
            //    };
            //    context.ExceptionHandled = true;
            //}
        }

    }
}
