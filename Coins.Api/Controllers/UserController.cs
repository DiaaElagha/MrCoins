using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Coins.Api.Model;
using Coins.Api.Services;
using Coins.Api.Utilities;
using Coins.Core.Constants;
using Coins.Core.Constants.Enums;
using Coins.Core.Helpers;
using Coins.Core.Helpers.Models;
using Coins.Core.Models.Domins.Auth;
using Coins.Core.Models.DtoAPI.User;
using Coins.Core.Services;
using Coins.Entities.Domins.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Coins.Api.Controllers
{
    public class UserController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IStoresService _storeService;

        public UserController(
            IAuthService authService,
            IUserService userService,
            IStoresService storeService,

            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper) : base(configuration, userManager, httpContextAccessor, mapper)
        {
            _authService = authService;
            _userService = userService;
            _storeService = storeService;
        }

        [HttpGet("/User")]
        public async Task<IActionResult> GetProfile()
        {
            ApplicationUser user = await GetCurrentUser();
            if (user != null)
            {
                UserProfileDto userProfile = _authService.GetUserProfile(user);
                return GetResponse(ResponseMessages.READ, true, userProfile);
            }
            else
                return GetResponse("Username is not valid", false, null);
        }

        // Update User Profile
        [HttpPut("/User")]
        public async Task<IActionResult> Update([FromBody] UserProfileDto userDto)
        {
            var userId = GetCurrentUserId();
            await _authService.Update(userId, userDto, null);
            return GetResponse(ResponseMessages.UPDATE, true, userDto);
        }

        [HttpGet("/User/QR")]
        public IActionResult QR()
        {
            var userId = GetCurrentUserId();
            string qrImage = ExtensionMethods.GenerateQR(userId.ToString());
            return GetResponse(ResponseMessages.READ, true, qrImage);
        }

        [HttpGet("/User/QR/{code}")]
        public IActionResult QR(Guid code)
        {
            string qrImage = ExtensionMethods.GenerateQR(code.ToString());
            return GetResponse(ResponseMessages.READ, true, qrImage);
        }

        [HttpGet("/User/Vouchers")]
        public async Task<IActionResult> Vouchers(int storeId, int size = 15, int page = 1)
        {
            var userId = GetCurrentUserId();

            var userVouchersItems = await _userService.Vouchers(storeId: storeId, userId: userId, size: size, page: page);

            var userVouchersItemsDto = _mapper.Map<List<UserVoucherDTO>>(userVouchersItems.Data);

            foreach (var item in userVouchersItemsDto)
            {
                SetVoucherStatus(item);
            }
            var resultData = new Wrapper<List<UserVoucherDTO>>
            {
                Data = userVouchersItemsDto,
                Pagination = userVouchersItems.Pagination
            };
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpGet("/User/Vouchers/{voucherId}")]
        public async Task<IActionResult> Voucher(int voucherId)
        {
            var userVouchersItem = await _userService.GetVoucherById(voucherId);
            var resultData = _mapper.Map<List<UserVoucherDTO>>(userVouchersItem);
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpPost("/User/Vouchers/Redeem/{voucherCode}")]
        public async Task<IActionResult> RedeemVoucher(int voucherCode)
        {
            var currentUserId = GetCurrentUserId();
            var resultUnlockVoucher = await _storeService.UnlockVoucher(userId: currentUserId, voucherId: voucherCode);
            if (resultUnlockVoucher)
                return GetResponse(ResponseMessages.Operation, true, resultUnlockVoucher);
            else
                return GetResponse(ResponseMessages.FAILED, false, resultUnlockVoucher);
        }

        [HttpPost("/User/SocialMedia/Claim")]
        public async Task<IActionResult> SocialMediaClaim(SocialType mediaId, int storeId)
        {
            var currentUserId = GetCurrentUserId();
            var resultConnect = await _storeService.SocialMediaActivityCliam(mediaId: ((int)mediaId), storeId: storeId, userId: currentUserId);
            return GetResponse(ResponseMessages.Operation, true, resultConnect);
        }

        [HttpPost("/User/SocialMedia/Connect/{media}")]
        public async Task<IActionResult> ConnectMedia(SocialType media, [FromBody] ConnectMediaDto model)
        {
            if (!ModelState.IsValid)
                return GetResponse(ResponseMessages.ModelStateInValid, false, General.GetValidationErrores(ModelState));
            var currentUserId = GetCurrentUserId();
            var resultObj = await _userService.ConnectMedia(currentUserId, (SocialType)media, model.MediaToken);
            return GetResponse(ResponseMessages.Operation, true, resultObj);
        }

        [HttpGet("/User/Notifications")]
        public async Task<IActionResult> Notifications(int size = 15, int page = 1)
        {
            var userId = GetCurrentUserId();
            var userNotifications = await _userService.GetNotifications(userId, size, page);
            var resultData = new Wrapper<List<NotificationDTO>>
            {
                Data = _mapper.Map<List<NotificationDTO>>(userNotifications.Data),
                Pagination = userNotifications.Pagination
            };
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        private void SetVoucherStatus(UserVoucherDTO userVoucher)
        {
            DateTimeOffset expiredDate = userVoucher.VoucherStartDate.AddDays(userVoucher.Voucher.VoucherExpiredAfterDay);
            userVoucher.VoucherExpiryDate = expiredDate;
            userVoucher.DaysLeftToExpiry = Convert.ToInt32((expiredDate - DateTimeOffset.Now).TotalDays);

            if (userVoucher.IsRedeem)
            {
                userVoucher.VoucherStatus = VoucherStatus.Used;
                return;
            }
            switch (userVoucher.DaysLeftToExpiry)
            {
                case > 0:
                    userVoucher.VoucherStatus = VoucherStatus.Active;
                    break;
                case < 0:
                    userVoucher.VoucherStatus = VoucherStatus.ExpiryDate;
                    break;
                default:
                    userVoucher.VoucherStatus = VoucherStatus.NotUsed;
                    break;
            }
            return;
        }

    }
}
