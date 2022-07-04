using Coins.Core.Helpers.Models;
using Coins.Core.Models.Domins.Auth;
using Coins.Core.Models.Domins.StoresInfo;
using Coins.Core.Models.DtoAPI.Store;
using Coins.Core.Models.DtoAPI.User;
using Coins.Entities.Domins.StoresInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coins.Core.Services
{
    public interface IStoresService
    {
        Task<double> AddRate(Guid userId, int storeBranchId, double rateValue);
        Task<bool> SocialMediaActivityCliam(int mediaId, int storeId, Guid userId);
        Task<Stores> CreateStore(Stores stores, string userName, string password);
        Task<StoreBranchs> GetStoreById(Guid userId, int Id, bool addToSearch);
        Task<Wrapper<List<StoreCategory>>> GetStoreCategories(int size, int page);
        Task<Wrapper<List<StoreProducts>>> GetStoreMenu(int storeId, int size, int page);
        Task<Wrapper<List<StoreBranchs>>> GetStores(StoresParamDto model);
        Task<Wrapper<List<StoreBranchs>>> GetStoresNearBy(StoresParamDto model);
        Task<bool> SetInvoice(Guid cashierId, SetInvoiceDto model);
        Task<bool> UnlockVoucher(Guid userId, int voucherId);
        Task<Wrapper<List<StoreBranchs>>> GetStoresMostVisited(StoresParamDto model);
        Task<Wrapper<StoreBranchs>> GetStoreAds(StoresParamAdsDto model);
        Task<Wrapper<List<StoreBranchs>>> GetStoresVisitedBefore(Guid userId, StoresParamDto model);
        Task<Wrapper<List<Voucher>>> GetFirstTimeVouchers(int size, int page);
    }
}
