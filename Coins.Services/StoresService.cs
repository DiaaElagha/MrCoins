using AutoMapper;
using Coins.Core;
using Coins.Core.Constants;
using Coins.Core.Constants.Enums;
using Coins.Core.Helpers;
using Coins.Core.Helpers.Models;
using Coins.Core.Models.Domins.Attachments;
using Coins.Core.Models.Domins.Auth;
using Coins.Core.Models.Domins.Social;
using Coins.Core.Models.Domins.StoresInfo;
using Coins.Core.Models.DtoAPI.Store;
using Coins.Core.Models.DtoAPI.User;
using Coins.Core.Services;
using Coins.Core.Services.ThiedParty;
using Coins.Entities.Domins.Auth;
using Coins.Entities.Domins.StoresInfo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coins.Services
{
    public class StoresService : IStoresService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StorageService _storage;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationsService _notificationService;
        public StoresService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            StorageService storage,
            INotificationsService notificationService)
        {
            this._unitOfWork = unitOfWork;
            this._storage = storage;
            this._userManager = userManager;
            this._notificationService = notificationService;
        }

        #region Get
        public async Task<Wrapper<List<StoreBranchs>>> GetStores(StoresParamDto model)
        {
            var skip = model.size * (model.page - 1);

            var IQueryableStoreBranchs = _unitOfWork.StoreBranchs.Filter(
                   totalPages: out var totalPages,
                   filter: x => (!x.IsDeleted) && (x.Store.IsPublish) && (x.Store.IsActive),
                   orderBy: x => x.OrderByDescending(v => v.CreateAt),
                   include: x => x.Include(v => v.Store).ThenInclude(v => v.StoreCategory));

            if (model.categoryId.HasValue)
            {
                IQueryableStoreBranchs = IQueryableStoreBranchs.Where(x => x.Store.StoreCategoryId == model.categoryId);
            }
            if (!string.IsNullOrEmpty(model.search))
            {
                IQueryableStoreBranchs = IQueryableStoreBranchs.Where(x =>
                    x.BranchNameAr.Contains(model.search) ||
                    x.BranchNameEn.Contains(model.search) ||
                    x.Store.StoreNameAr.Contains(model.search) ||
                    x.Store.StoreNameEn.Contains(model.search)
                    );
            }
            if (model.services is not null)
            {
                if (model.services.Length > 0)
                {
                    IQueryableStoreBranchs = IQueryableStoreBranchs.Include(x => x.StoreBranchsAdvantagesList).Where(x => model.services.Intersect(
                        x.StoreBranchsAdvantagesList.Select(x => x.AdvantageId).ToArray()).Any());
                }
            }
            if (model.userLatitude.HasValue && model.userLongitude.HasValue)
            {

            }

            var listStoreBranchs = await IQueryableStoreBranchs.Skip(skip).Take(model.size).ToListAsync();

            return new Wrapper<List<StoreBranchs>>
            {
                Data = listStoreBranchs,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }

        public async Task<Wrapper<List<StoreBranchs>>> GetStoresNearBy(StoresParamDto model)
        {
            var skip = model.size * (model.page - 1);

            var IQueryableStoreBranchs = _unitOfWork.StoreBranchs.Filter(
                   totalPages: out var totalPages,
                   filter: x => (!x.IsDeleted) && (x.Store.IsPublish) && (x.Store.IsActive),
                   orderBy: x => x.OrderByDescending(v => v.CreateAt),
                   include: x => x.Include(v => v.Store).ThenInclude(v => v.StoreCategory));

            if (model.userLatitude.HasValue && model.userLongitude.HasValue && model.distance.HasValue)
            {
                Point userPoint = new Point(model.userLatitude.Value, model.userLongitude.Value);
                IQueryableStoreBranchs = IQueryableStoreBranchs.Where(x => DistanceOp.IsWithinDistance(x.Location, userPoint, model.distance.Value));
            }

            if (model.categoryId.HasValue)
            {
                IQueryableStoreBranchs = IQueryableStoreBranchs.Where(x => x.Store.StoreCategoryId == model.categoryId);
            }
            if (!string.IsNullOrEmpty(model.search))
            {
                IQueryableStoreBranchs = IQueryableStoreBranchs.Where(x =>
                    x.BranchNameAr.Contains(model.search) ||
                    x.BranchNameEn.Contains(model.search) ||
                    x.Store.StoreNameAr.Contains(model.search) ||
                    x.Store.StoreNameEn.Contains(model.search)
                    );
            }
            if (model.services is not null)
            {
                if (model.services.Length > 0)
                {
                    IQueryableStoreBranchs = IQueryableStoreBranchs.Include(x => x.StoreBranchsAdvantagesList).Where(x => model.services.Intersect(
                        x.StoreBranchsAdvantagesList.Select(x => x.AdvantageId).ToArray()).Any());
                }
            }

            var listStoreBranchs = await IQueryableStoreBranchs.Skip(skip).Take(model.size).ToListAsync();

            return new Wrapper<List<StoreBranchs>>
            {
                Data = listStoreBranchs,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }

        public async Task<Wrapper<List<StoreBranchs>>> GetStoresMostVisited(StoresParamDto model)
        {
            var skip = model.size * (model.page - 1);

            var listMostVisitedBranchs = await _unitOfWork.UserCoins.Get()
                .GroupBy(x => x.StoreBranchId)
                .Select(x => new { key = x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count)
                .Skip(skip).Take(model.size)
                .Select(x => x.key).ToListAsync();

            var IQueryableStoreBranchs = _unitOfWork.StoreBranchs.Filter(
                  totalPages: out var totalPages,
                  filter: x => (!x.IsDeleted) && (x.Store.IsPublish) && (x.Store.IsActive)
                  && listMostVisitedBranchs.Contains(x.BranchId),
                  orderBy: x => x.OrderByDescending(v => v.CreateAt),
                  include: x => x.Include(v => v.Store).ThenInclude(v => v.StoreCategory));

            var listStoreBranchs = await IQueryableStoreBranchs.Skip(skip).Take(model.size).ToListAsync();

            return new Wrapper<List<StoreBranchs>>
            {
                Data = listStoreBranchs,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }

        public async Task<Wrapper<List<StoreBranchs>>> GetStoresVisitedBefore(Guid userId, StoresParamDto model)
        {
            var skip = model.size * (model.page - 1);

            var IQueryableStoreBranchs = _unitOfWork.UserStores.Filter(
                   totalPages: out var totalPages,
                   filter: x => (x.UserId == userId) && (!x.IsDeleted) && (x.LastVisitStoreBranch.Store.IsActive),
                   orderBy: x => x.OrderByDescending(v => v.CreateAt),
                   include: x => x.Include(v => v.LastVisitStoreBranch).ThenInclude(v => v.Store).ThenInclude(v => v.StoreCategory));

            var listStoreBranchs = await IQueryableStoreBranchs.Select(x => x.LastVisitStoreBranch).Skip(skip).Take(model.size).ToListAsync();

            return new Wrapper<List<StoreBranchs>>
            {
                Data = listStoreBranchs,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }

        public async Task<Wrapper<List<StoreCategory>>> GetStoreCategories(int size, int page)
        {
            var skip = size * (page - 1);

            var listCategories = await _unitOfWork.StoreCategory.Filter(
                   totalPages: out var totalPages,
                   filter: x => (!x.IsDeleted),
                   skip: skip,
                   take: size,
                   orderBy: x => x.OrderByDescending(v => v.CreateAt)).ToListAsync();

            return new Wrapper<List<StoreCategory>>
            {
                Data = listCategories,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }

        public async Task<Wrapper<List<StoreProducts>>> GetStoreMenu(int storeId, int size, int page)
        {
            var skip = size * (page - 1);
            var listStoreBranchs = await _unitOfWork.StoreProducts.Filter(
                   totalPages: out var totalPages,
                   filter: x => (!x.IsDeleted) && (x.StoreId == storeId),
                   skip: skip,
                   take: size,
                   orderBy: x => x.OrderByDescending(v => v.CreateAt),
                   include: x => x.Include(v => v.Store).ThenInclude(v => v.StoreCategory).Include(v => v.Store.StoreRateList)).ToListAsync();
            return new Wrapper<List<StoreProducts>>
            {
                Data = listStoreBranchs,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }

        public async Task<StoreBranchs> GetStoreById(Guid userId, int Id, bool addToSearch)
        {
            var storeBranchItem = await _unitOfWork.StoreBranchs.GetStoreBranchById(Id);
            if (storeBranchItem is null)
                return null;
            // Add visit to StoreBranch
            if (addToSearch)
            {
                storeBranchItem.NumOfSearch = storeBranchItem.NumOfSearch + 1;
                _unitOfWork.StoreBranchs.Update(storeBranchItem);
                await _unitOfWork.CommitAsync();
            }

            var checkIfFirstVisit = await _unitOfWork.UserStores.GetSingleAsync(x => x.UserId == userId && x.StoreId == Id);
            // To create First Visit Voucher
            if (checkIfFirstVisit is null)
            {
                if (storeBranchItem.Store.FirstTimeVoucherId.HasValue)
                {
                    UserVouchers userVoucherFirstVisit = new UserVouchers
                    {
                        VoucherId = storeBranchItem.Store.FirstTimeVoucherId.Value,
                        UserId = userId,
                        CreateByUserId = userId
                    };
                    await _unitOfWork.UserVouchers.AddAsync(userVoucherFirstVisit);
                    await _unitOfWork.CommitAsync();
                }
            }

            //Join SocialTypesStores
            {
                var socialTypesStoreList = await _unitOfWork.SocialTypesStores.Get().Where(x => x.IsActive).Include(x => x.Voucher).ToListAsync();
                storeBranchItem.Store.SocialTypesStoresList = socialTypesStoreList;
                foreach (var item in storeBranchItem.Store.SocialTypesStoresList)
                {
                    item.IsUsed = _unitOfWork.UserSocialStore.Any(x => x.UserId == userId && x.StoreBranchId == storeBranchItem.BranchId && x.SocialType == item.SocialType);
                }
            }
            //Join Store Vouchers
            {
                var vouchersStoreList = await _unitOfWork.Vouchers.Get().Where(x => x.StoreId == Id && x.IsActive).ToListAsync();
                storeBranchItem.Store.VouchersList = vouchersStoreList;
            }
            //Join StoreProducts
            {
                var storeProductsList = await _unitOfWork.StoreProducts.Get()
                    .Where(x => x.StoreId == storeBranchItem.StoreId).ToListAsync();
                storeBranchItem.Store.StoreProductsList = storeProductsList;
            }
            storeBranchItem.ReferrralCode = checkIfFirstVisit?.ReferrralCode ?? null;


            return storeBranchItem;
        }

        public async Task<Wrapper<List<Voucher>>> GetFirstTimeVouchers(int size, int page)
        {
            var skip = size * (page - 1);

            var IQueryableVouchers = _unitOfWork.Vouchers.Filter(
                   totalPages: out var totalPages,
                   filter: x => (x.VoucherType == VoucherType.FirstTime) && (x.Store.IsActiveFirstTimeVoucher) && (!x.IsDeleted),
                   orderBy: x => x.OrderByDescending(v => v.CreateAt),
                   include: x => x.Include(v => v.Store).ThenInclude(v => v.StoreCategory));

            var listVouchers = await IQueryableVouchers.Skip(skip).Take(size).ToListAsync();
            return new Wrapper<List<Voucher>>
            {
                Data = listVouchers,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }
        #endregion

        #region UnlockVoucher

        public async Task<bool> UnlockVoucher(Guid userId, int voucherId)
        {
            var voucherItem = await _unitOfWork.Vouchers.Get().Include(x => x.Store).SingleOrDefaultAsync(x => x.VoucherId == voucherId);

            if (voucherItem.VoucherType != VoucherType.Normal)
                return false;

            var userStoreBranch = await _unitOfWork.UserStores.Get().Where(x => x.UserId == userId && x.StoreId == voucherItem.StoreId).Include(x => x.UserStore).SingleOrDefaultAsync();
            if (userStoreBranch is null)
                return false;

            var sumCoinsUser = await _unitOfWork.UserCoins.GetSumCoins(userId: userId, store: voucherItem.Store);

            if (sumCoinsUser < voucherItem.NumOfCoins)
            {
                // SendNotification 
                // --> NotificationsConst.FailedUnlockVoucher;
                await _notificationService.Send(userStoreBranch.UserStore.FcmToken, NotificationsConst.FailedUnlockVoucher());
            }

            await CreateUserVouchers(userId, voucherId);

            // Deduct X Coins from Wallet user

            int numCoinsDeducted = voucherItem.NumOfCoins; // 500
            int numCoinsCollected = 0;
            var userCoinsItems = await _unitOfWork.UserCoins.Get().Where(x => x.StoreId == voucherItem.StoreId && x.Remaining > 0).OrderByDescending(x => x.CoinExpiryDate).ToListAsync();
            foreach (var item in userCoinsItems)
            {
                if (numCoinsDeducted <= numCoinsCollected)
                    break;
                numCoinsCollected += item.NumberOfCoinCollected; // 100
                int remainigCalculated = item.NumberOfCoinCollected - numCoinsCollected; // 100 - 0 = 100
                if (remainigCalculated <= 0)
                {
                    item.Remaining = 0;
                }
                else
                {
                    item.Remaining = remainigCalculated;
                }
                _unitOfWork.UserCoins.Update(item);
            }
            await _unitOfWork.CommitAsync();

            await UpdateTotalCoins(userId, voucherItem.Store, userStoreBranch, sumCoinsUser);

            // SendNotification 
            // --> NotificationsConst.SuccessUnlockVoucher;
            await _notificationService.Send(userStoreBranch.UserStore.FcmToken, NotificationsConst.SuccessUnlockVoucher());
            return true;
        }

        private async Task CreateUserVouchers(Guid userId, int voucherId)
        {
            var userVoucher = new UserVouchers
            {
                VoucherId = voucherId,
                UserId = userId,
                CreateByUserId = userId
            };
            await _unitOfWork.UserVouchers.AddAsync(userVoucher);
            await _unitOfWork.CommitAsync();
        }

        #endregion

        #region SetInvoice by cashier
        public async Task<bool> SetInvoice(Guid cashierId, SetInvoiceDto model)
        {
            try
            {
                var cashierItem = await _userManager.Users
                                .Include(x => x.Store)
                                .Include(x => x.StoreBranch)
                                .SingleOrDefaultAsync(x => x.Id == cashierId);

                var userStoreBranch = await _unitOfWork.UserStores
                    .GetSingleAsync(x => x.UserId == model.UserId && x.StoreId == cashierItem.StoreId);

                DateTimeOffset lastVisitStoreAt = DateTimeOffset.Now;

                if (userStoreBranch is null)
                    userStoreBranch = await CreateUserStore(model, cashierItem);
                else
                {
                    lastVisitStoreAt = userStoreBranch.LastVisitAt.Value;
                    await UpdateUserStore(cashierItem, userStoreBranch);
                }

                int numOfCoinCollected = Convert.ToInt16(model.InvoiceValue * cashierItem.Store.DefineCurrentCurrencyToCoins);

                numOfCoinCollected = ExchangeCoins(lastVisitStoreAt, numOfCoinCollected);

                await CreateUserCoins(cashierId, model, cashierItem.Store, numOfCoinCollected, cashierItem.StoreBranchId);

                await UpdateTotalCoins(model.UserId, cashierItem.Store, userStoreBranch);

                // SendNotification 
                // NotificationsConst.ReceivedCoins;
                await _notificationService.Send(userStoreBranch.UserStore.FcmToken, NotificationsConst.ReceivedCoins(numCoins: numOfCoinCollected.ToString(), storeName: cashierItem.Store.StoreNameAr));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task UpdateTotalCoins(Guid userId, Stores store, UserStores userStoreBranch, int? totalCoins = null)
        {
            if (!totalCoins.HasValue)
            {
                totalCoins = await _unitOfWork.UserCoins.GetSumCoins(userId: userId, store: store);
            }

            userStoreBranch.TotalCoins = totalCoins.Value;
            _unitOfWork.UserStores.Update(userStoreBranch);
            await _unitOfWork.CommitAsync();
        }

        private async Task UpdateUserStore(ApplicationUser cashierItem, UserStores userStoreBranch)
        {
            if (userStoreBranch is not null)
            {
                userStoreBranch.LastVisitStoreBranchId = cashierItem.StoreBranchId.Value;
                userStoreBranch.NumOfVisitStore = userStoreBranch.NumOfVisitStore + 1;
                userStoreBranch.LastVisitAt = DateTimeOffset.Now;
                _unitOfWork.UserStores.Update(userStoreBranch);
                await _unitOfWork.CommitAsync();
            }
        }

        private async Task CreateUserCoins(Guid userId, SetInvoiceDto model, Stores store, int numOfCoinCollected, int? storeBranchId = null)
        {
            UserCoins userCoins = new UserCoins
            {
                NumberOfCoinCollected = numOfCoinCollected,
                StoreBranchId = storeBranchId,
                StoreId = store.StoreId,
                UserId = model.UserId,
                InvoiceValue = model.InvoiceValue,
                InvoiceNumber = model.InvoiceNumber,
                CreateByUserId = userId
            };
            if (store.ExpiedCoinsAfterDays.HasValue)
                userCoins.CoinExpiryDate = DateTimeOffset.Now.AddDays(store.ExpiedCoinsAfterDays.Value);

            await _unitOfWork.UserCoins.AddAsync(userCoins);
            await _unitOfWork.CommitAsync();
        }

        private static int ExchangeCoins(DateTimeOffset lastVisitStoreAt, int numOfCoinCollected)
        {
            int daysBetweenVisits = Convert.ToInt16(Math.Round((DateTimeOffset.Now - lastVisitStoreAt).TotalDays));

            switch (daysBetweenVisits)
            {
                case 0:
                    break;
                case <= 7:
                    numOfCoinCollected = Convert.ToInt16(numOfCoinCollected * 2);
                    break;
                case > 7 and <= 14:
                    numOfCoinCollected = Convert.ToInt16(numOfCoinCollected * 1.5);
                    break;
                default:
                    break;
            }

            return numOfCoinCollected;
        }

        private async Task<UserStores> CreateUserStore(SetInvoiceDto model, ApplicationUser cashierItem)
        {
            UserStores userStoreBranch = new UserStores
            {
                UserId = model.UserId,
                StoreId = cashierItem.StoreId.Value,
                LastVisitStoreBranchId = cashierItem.StoreBranchId.Value,
                LastVisitAt = DateTimeOffset.Now,
                NumOfVisitStore = 1,
                CreateByUserId = cashierItem.Id,
                ReferrralCode = string.Format("{0}{1}", cashierItem.StoreId.Value, model.UserId.ToString().Replace("-", "").Substring(0, 8))
            };
            await _unitOfWork.UserStores.AddAsync(userStoreBranch);
            await _unitOfWork.CommitAsync();
            return userStoreBranch;
        }
        #endregion

        #region Rateing
        public async Task<double> AddRate(Guid userId, int storeBranchId, double rateValue)
        {
            var storeRateResult = await CreateStoreRate(userId, storeBranchId, rateValue);
            var avgRateResult = await UpdateAvgStoreBranchRate(storeBranchId);
            return avgRateResult;
        }

        private async Task<double> UpdateAvgStoreBranchRate(int storeBranchId)
        {
            var avgRateValue = await _unitOfWork.StoreRate.Get().Where(x => x.StoreBranchId == storeBranchId).AsNoTracking().AverageAsync(x => x.RateValue);

            var storeBranchItem = await _unitOfWork.StoreBranchs.GetByIdAsync(storeBranchId);
            storeBranchItem.AvgRate = avgRateValue;
            _unitOfWork.StoreBranchs.Update(storeBranchItem);
            await _unitOfWork.CommitAsync();
            return avgRateValue;
        }

        private async Task<StoreRate> CreateStoreRate(Guid userId, int storeBranchId, double rateValue)
        {
            var storeRateResult = await _unitOfWork.StoreRate.GetSingleAsync(x => x.UserId == userId && x.StoreBranchId == storeBranchId);
            if (storeRateResult is null)
            {
                storeRateResult = new StoreRate
                {
                    StoreBranchId = storeBranchId,
                    UserId = userId,
                    CreateByUserId = userId,
                    RateValue = rateValue
                };
                await _unitOfWork.StoreRate.AddAsync(storeRateResult);
                await _unitOfWork.CommitAsync();
            }
            else
            {
                storeRateResult.RateValue = rateValue;
                _unitOfWork.StoreRate.Update(storeRateResult);
                await _unitOfWork.CommitAsync();
            }
            return storeRateResult;
        }

        #endregion

        #region Media & friends
        public async Task<bool> SocialMediaActivityCliam(int mediaId, int storeId, Guid userId)
        {
            var storeBranchItem = await _unitOfWork.StoreBranchs.Get().Where(x => x.BranchId == storeId).Include(x => x.Store).SingleOrDefaultAsync();
            var socialTypeStore = await _unitOfWork.SocialTypesStores.GetSingleAsync(x => x.StoreBranchId == storeBranchItem.BranchId && x.StoreId == storeBranchItem.StoreId && ((int)x.SocialType) == mediaId);
            var checkSocialStoreItem = _unitOfWork.UserSocialStore.Any(x => ((int)x.SocialType) == mediaId && x.StoreBranchId == storeBranchItem.BranchId && x.UserId == userId);
            if (checkSocialStoreItem)
            {
                return false;
            }
            else
            {
                var userStoreBranch = await _unitOfWork.UserStores
                 .GetSingleAsync(x => x.UserId == userId && x.StoreId == storeBranchItem.Store.StoreId);

                int numOfCoinCollected = socialTypeStore?.RewardNumberOfCoins ?? 0;
                if (numOfCoinCollected == 0)
                    return false;

                await CreateUserSocialStore(mediaId, userId, storeBranchItem);

                await CreateUserCoins(userId, new SetInvoiceDto { UserId = userId }, storeBranchItem.Store, numOfCoinCollected);

                await UpdateTotalCoins(userId, storeBranchItem.Store, userStoreBranch);

                // SendNotification 
                // NotificationsConst.ReceivedCoins;
                var userItem = await _userManager.FindByIdAsync(userId.ToString());
                await _notificationService.Send(userItem.FcmToken, NotificationsConst.ReceivedCoins(numCoins: numOfCoinCollected.ToString(), storeName: storeBranchItem.Store.StoreNameAr));
            }
            return true;
        }

        public async Task<bool> ReferralFriend(Guid userId, int storeId)
        {
            var storeBranchItem = await _unitOfWork.StoreBranchs.Get().Where(x => x.BranchId == storeId).Include(x => x.Store).SingleOrDefaultAsync();

            var checkReferralFriend = false;

            if (checkReferralFriend)
            {
                return false;
            }
            else
            {
                var userStoreBranch = await _unitOfWork.UserStores
                 .GetSingleAsync(x => x.UserId == userId && x.StoreId == storeBranchItem.Store.StoreId);

                int numOfCoinCollected = storeBranchItem?.Store?.DefineNumOfReferralCustomerCoins ?? 0;
                if (numOfCoinCollected == 0)
                    return false;

                await CreateUserCoins(userId, new SetInvoiceDto { UserId = userId }, storeBranchItem.Store, numOfCoinCollected);

                await UpdateTotalCoins(userId, storeBranchItem.Store, userStoreBranch);

                // SendNotification 
                // NotificationsConst.ReceivedCoins;
                var userItem = await _userManager.FindByIdAsync(userId.ToString());
                await _notificationService.Send(userItem.FcmToken, NotificationsConst.ReceivedCoins(numCoins: numOfCoinCollected.ToString(), storeName: storeBranchItem.Store.StoreNameAr));
            }
            return await Task.FromResult(true);
        }

        private async Task CreateUserSocialStore(int mediaId, Guid userId, StoreBranchs storeBranchItem)
        {
            UserSocialStore userSocialStore = new UserSocialStore
            {
                UserId = userId,
                StoreBranchId = storeBranchItem.BranchId,
                SocialType = (SocialType)mediaId,
                CreateByUserId = userId
            };
            await _unitOfWork.UserSocialStore.AddAsync(userSocialStore);
            await _unitOfWork.CommitAsync();
        }
        #endregion

        #region CRUD Store
        public async Task<Stores> CreateStore(Stores stores, string userName, string password)
        {
            await _unitOfWork.Stores.AddAsync(stores);
            await _unitOfWork.CommitAsync();

            {
                // CREATE NEW LOGIN STORE USER
                var appUser = new ApplicationUser
                {
                    UserName = userName,
                    FullName = stores.StoreNameAr,
                    IsActive = stores.IsActive,
                    Role = UsersRoles.StoreAdmin,
                    StoreId = stores.StoreId
                };
                var resultCreateAppUser = await _userManager.CreateAsync(appUser, password);

                // CREATE NEW DEFULT MAIN Branch STORE
                var mainBranch = new StoreBranchs
                {
                    StoreId = stores.StoreId,
                    IsMainBranch = true,
                    BranchNameAr = stores.StoreNameAr,
                    BranchNameEn = stores.StoreNameEn,
                    CreateByUserId = stores.CreateByUserId
                };
                await _unitOfWork.StoreBranchs.AddAsync(mainBranch);
                await _unitOfWork.CommitAsync();

            }
            return stores;
        }
        #endregion

        #region Ads stores
        public async Task<Wrapper<StoreBranchs>> GetStoreAds(StoresParamAdsDto model)
        {
            Point userPoint = new Point(model.userLatitude, model.userLongitude);
            var IQueryableStoreBranchs = _unitOfWork.StoreBranchs.Filter(
                   totalPages: out var totalPages,
                   filter: x => (DistanceOp.IsWithinDistance(x.Location, userPoint, model.distance)) && (!x.IsDeleted) && (x.Store.IsPublish) && (x.Store.IsActive) && (x.Store.StoreCategoryId == model.categoryId),
                   orderBy: x => x.OrderByDescending(v => v.CreateAt),
                   include: x => x.Include(v => v.Store).ThenInclude(v => v.StoreCategory));

            int count = IQueryableStoreBranchs.Count(); // 1st round-trip
            int index = new Random().Next(count);

            var listStoreBranchs = await IQueryableStoreBranchs.Skip(index).FirstOrDefaultAsync();

            return new Wrapper<StoreBranchs>
            {
                Data = listStoreBranchs,
                Pagination = new Pagination { TotalPages = totalPages }
            };
        }

        #endregion
    }
}
