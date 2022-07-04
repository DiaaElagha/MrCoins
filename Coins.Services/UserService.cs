using AutoMapper;
using Coins.Core;
using Coins.Core.Constants;
using Coins.Core.Constants.Enums;
using Coins.Core.Helpers.Models;
using Coins.Core.Models.Domins.Attachments;
using Coins.Core.Models.Domins.Auth;
using Coins.Core.Models.Domins.StoresInfo;
using Coins.Core.Models.DtoAPI.User;
using Coins.Core.Services;
using Coins.Core.Services.ThiedParty;
using Coins.Entities.Domins.Notification;
using Coins.Entities.Domins.StoresInfo;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coins.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationsService _notificationService;
        public UserService(IUnitOfWork unitOfWork, INotificationsService notificationService)
        {
            this._unitOfWork = unitOfWork;
            this._notificationService = notificationService;
        }

        public async Task<Wrapper<List<UserVouchers>>> Vouchers(int storeId, Guid userId, int size = 15, int page = 1)
        {
            var skip = size * (page - 1);
            var listUserVouchers = await _unitOfWork.UserVouchers.Filter(
                   totalPages: out var totalPages,
                   filter: x => (!x.IsDeleted) && (x.UserId == userId) && (x.Voucher.StoreId == storeId),
                   skip: skip,
                   take: size,
                   orderBy: x => x.OrderByDescending(v => v.CreateAt),
                   include: x => x.Include(v => v.Voucher)).ToListAsync();
            return new Wrapper<List<UserVouchers>>
            {
                Data = listUserVouchers,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }

        public async Task<UserVouchers> GetVoucherById(int voucherId)
        {
            var userVoucher = await _unitOfWork.UserVouchers.Get().Include(v => v.Voucher).SingleOrDefaultAsync(x => x.VoucherId == voucherId);
            return userVoucher;
        }

        public async Task<UserSocialLogin> ConnectMedia(Guid userId, SocialType socialType, string mediaToken)
        {
            var itemUserSocialLogin = new UserSocialLogin
            {
                LoginToken = mediaToken,
                SocialType = socialType,
                UserId = userId,
                CreateByUserId = userId,
                CreateAt = DateTime.Now,
            };
            await _unitOfWork.UserSocialLogin.AddAsync(itemUserSocialLogin);
            await _unitOfWork.CommitAsync();
            return itemUserSocialLogin;
        }

        public async Task<Wrapper<List<Notifications>>> GetNotifications(Guid userId, int size = 15, int page = 1)
        {
            var skip = size * (page - 1);
            var userNotifications = await _unitOfWork.Notifications.Filter(
                   totalPages: out var totalPages,
                   filter: x => x.ReceverId == userId,
                   skip: skip,
                   take: size,
                   orderBy: x => x.OrderByDescending(v => v.SendDateAt)).ToListAsync();
            return new Wrapper<List<Notifications>>
            {
                Data = userNotifications,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }

    }
}
