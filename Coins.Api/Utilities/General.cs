using Coins.Api.Models.Base;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Coins.Api.Utilities
{
    public class General
    {
        public static APIResponse GetValidationErrores(ModelStateDictionary ModelState)
        {
            APIResponse response = new APIResponse();

            response.Status = false;
            response.Message = "Some Filed Required";

            var errorList = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

            response.Data = errorList;
            return response;
        }


    }
}
