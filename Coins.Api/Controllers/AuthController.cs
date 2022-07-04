using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Coins.Api.Model;
using Coins.Api.Services;
using Coins.Api.Utilities;
using Coins.Core.Constants;
using Coins.Core.Helpers;
using Coins.Core.Models.DtoAPI.Auth;
using Coins.Core.Models.DtoAPI.User;
using Coins.Entities.Domins.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Coins.Api.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AuthController(
            IAuthService authService,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper) : base(configuration, userManager, httpContextAccessor, mapper)
        {
            _authService = authService;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpPost("/Auth/Login/Anonymous")]
        [AllowAnonymous]
        public async Task<IActionResult> Anonymous([FromBody] AnonymousDto model)
        {
            if (!ModelState.IsValid)
                return GetResponse("Some fields are required", false, General.GetValidationErrores(ModelState));

            var userProfile = await _authService.Anonymous(model.DeviceId);
            var baseUserProfile = new BaseUserProfileDto
            {
                DeviceId = userProfile.DeviceId,
                AccessToken = userProfile.AccessToken
            };
            return GetResponse(ResponseMessages.Operation, true, baseUserProfile);
        }

        // Request a Verification Code
        [HttpPost("/Auth/Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return GetResponse("Some fields are required", false, General.GetValidationErrores(ModelState));

            var userProfile = await _authService.VerificationUser(model.MobileNo, model.DeviceId);
            var baseUserProfile = new BaseUserProfileDto
            {
                MobileNo = userProfile.UserName,
                DeviceId = userProfile.DeviceId,
                AccessToken = userProfile.AccessToken
            };
            return GetResponse(ResponseMessages.READ, true, baseUserProfile);
        }

        [HttpPost("/Auth/Verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyLoginDto model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser userProfile = await _authService.Authenticate(model);
                if (userProfile == null)
                    return GetResponse("Mobile number or Code is incorrect.", false, null);

                if (!userProfile.IsActive)
                    return GetResponse("User is inactive", false, null);

                if (userProfile.ExpiryDate.HasValue)
                    if (userProfile.ExpiryDate < DateTime.Now)
                        return GetResponse("User is Expired", false, null);
                var baseUserProfile = new BaseUserProfileDto
                {
                    MobileNo = userProfile.UserName,
                    DeviceId = userProfile.DeviceId,
                    AccessToken = userProfile.AccessToken
                };
                return GetResponse("You are logged in successfully", true, baseUserProfile);
            }
            return GetResponse("Some fields are required", false, General.GetValidationErrores(ModelState));
        }

       
        [HttpPost("/Auth/ChangePhone")]
        public async Task<IActionResult> ChangeMobile([FromBody] ChangeMobileDto model)
        {
            if (!ModelState.IsValid)
                return GetResponse(ResponseMessages.ModelStateInValid, false, General.GetValidationErrores(ModelState));

            ApplicationUser user = await GetCurrentUser();
            if (user == null)
                return GetResponse(ResponseMessages.FAILED, false, null);

            if (await _userManager.Users.AnyAsync(x => x.UserName.Equals(model.MobileNo)))
                return GetResponse("Mobile number is already taken", false, null);

            await _authService.SendVerfiyMobile(user, model.MobileNo);
            return GetResponse(ResponseMessages.Operation, true, true);
        }

        [HttpPost("/Auth/ChangePhone/Verify")]
        public async Task<IActionResult> VerifyChangeMobile([FromBody] VerifyChangeMobileDto model)
        {
            if (!ModelState.IsValid)
                return GetResponse(ResponseMessages.ModelStateInValid, false, General.GetValidationErrores(ModelState));

            ApplicationUser user = await GetCurrentUser();
            if (user == null)
                return GetResponse(ResponseMessages.FAILED, false, null);

            bool result = await _authService.CheckVerfiyMobile(user, model.Code);
            if (result)
                return GetResponse(ResponseMessages.UPDATE, true, true);
            else
                return GetResponse("Invalid OTP Code", false, null);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            ApplicationUser user = await GetCurrentUser();
            user.FcmToken = null;
            user.AccessToken = null;
            user.IsVerify = false;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return GetResponse(ResponseMessages.Operation, true, null);
            }
            return GetResponse(ResponseMessages.FAILED, true, null);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshFcmToken([FromBody] RefreshFcmTokenDto model)
        {
            ApplicationUser user = await GetCurrentUser();
            user.FcmToken = model.FcmToken;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return GetResponse(ResponseMessages.UPDATE, true, true);
            }
            return GetResponse(ResponseMessages.FAILED, false, false);
        }

    }
}
