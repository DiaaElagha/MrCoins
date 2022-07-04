using AutoMapper;
using Coins.Api.Utilities;
using Coins.Core.Constants;
using Coins.Core.Helpers.Models;
using Coins.Core.Models.Domins.StoresInfo;
using Coins.Core.Models.DtoAPI.Store;
using Coins.Core.Models.DtoAPI.User;
using Coins.Core.Services;
using Coins.Entities.Domins.Auth;
using Coins.Entities.Domins.StoresInfo;
using Coins.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Coins.Api.Controllers
{
    public class StoresController : BaseController
    {
        private readonly IStoresService _storesService;
        private readonly StorageService _storageService;

        public StoresController(
            IStoresService storesService,

            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            StorageService storage) : base(configuration, userManager, httpContextAccessor, mapper)
        {
            _storesService = storesService;
            _storageService = storage;
        }

        [HttpGet("/Stores")]
        public async Task<IActionResult> GetStores([FromQuery] StoresParamDto model)
        {
            var storeBranchsItems = await _storesService.GetStores(model);
            var resultData = new Wrapper<List<StoreBranchDto>>
            {
                Data = _mapper.Map<List<StoreBranchDto>>(storeBranchsItems.Data),
                Pagination = storeBranchsItems.Pagination
            };
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpGet("/Stores/{Id}")]
        public async Task<IActionResult> GetStoreById(int Id, bool fromSearch = false)
        {
            var currentUserId = GetCurrentUserId();
            var storeItem = await _storesService.GetStoreById(currentUserId, Id, fromSearch);
            var resultData = _mapper.Map<StoreBranchDto>(storeItem);
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpGet("/Stores/Nearby")]
        public async Task<IActionResult> GetStoresNearby([FromQuery] StoresParamDto model)
        {
            var storeBranchsItems = await _storesService.GetStoresNearBy(model);
            var resultData = new Wrapper<List<StoreBranchDto>>
            {
                Data = _mapper.Map<List<StoreBranchDto>>(storeBranchsItems.Data),
                Pagination = storeBranchsItems.Pagination
            };
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpGet("/Stores/VisitedBefore")]
        public async Task<IActionResult> GetStoresVisitedBefore([FromQuery] StoresParamDto model)
        {
            var storeBranchsItems = await _storesService.GetStoresVisitedBefore(GetCurrentUserId(), model);
            var resultData = new Wrapper<List<StoreBranchDto>>
            {
                Data = _mapper.Map<List<StoreBranchDto>>(storeBranchsItems.Data),
                Pagination = storeBranchsItems.Pagination
            };
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpGet("/Stores/MostVisited")]
        public async Task<IActionResult> GetStoresMostVisited([FromQuery] StoresParamDto model)
        {
            var storeBranchsItems = await _storesService.GetStoresMostVisited(model);
            var resultData = new Wrapper<List<StoreBranchDto>>
            {
                Data = _mapper.Map<List<StoreBranchDto>>(storeBranchsItems.Data),
                Pagination = storeBranchsItems.Pagination
            };
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpGet("/Stores/FT-Vouchers")]
        public async Task<IActionResult> GetFirstTimeVouchers(int size = 15, int page = 1)
        {
            var storeVouchers = await _storesService.GetFirstTimeVouchers(size, page);
            var resultData = new Wrapper<List<StoreBranchDto>>
            {
                Data = _mapper.Map<List<StoreBranchDto>>(storeVouchers.Data),
                Pagination = storeVouchers.Pagination
            };
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpGet("/Stores/Categories")]
        public async Task<IActionResult> GetCategories(int size = 15, int page = 1)
        {
            var categoriesItems = await _storesService.GetStoreCategories(size: size, page: page);
            var resultData = new Wrapper<List<StoreCategoryDto>>
            {
                Data = _mapper.Map<List<StoreCategoryDto>>(categoriesItems.Data),
                Pagination = categoriesItems.Pagination
            };
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpGet("/Stores/Menu/{storeId}")]
        public async Task<IActionResult> GetStoreMenu(int storeId, int size = 15, int page = 1)
        {
            var storeProdectsItems = await _storesService.GetStoreMenu(storeId: storeId, size: size, page: page);
            var resultData = new Wrapper<List<StoreProductDTO>>
            {
                Data = _mapper.Map<List<StoreProductDTO>>(storeProdectsItems.Data),
                Pagination = storeProdectsItems.Pagination
            };
            return GetResponse(ResponseMessages.READ, true, resultData);
        }

        [HttpPost("/Stores/Invoice")]
        public async Task<IActionResult> SetInvoice([FromBody] SetInvoiceDto model)
        {
            if (ModelState.IsValid)
            {
                var currentUserId = GetCurrentUserId();

                var resultSetInvoice = await _storesService.SetInvoice(cashierId: currentUserId, model: model);
                if (resultSetInvoice)
                    return GetResponse(ResponseMessages.Operation, true, resultSetInvoice);
                else
                    return GetResponse(ResponseMessages.FAILED, false, resultSetInvoice);
            }
            return GetResponse(ResponseMessages.ModelStateInValid, false, General.GetValidationErrores(ModelState));
        }

        [HttpPost("/Stores/AddRate")]
        public async Task<IActionResult> AddRate([FromBody] AddRateDto model)
        {
            var currentUserId = GetCurrentUserId();
            var avgRateResult = await _storesService.AddRate(userId: currentUserId, storeBranchId: model.storeBranchId, rateValue: model.rateValue);
            return GetResponse(ResponseMessages.Operation, true, avgRateResult);
        }

        [HttpGet("/File/{id}")]
        public async Task<IActionResult> GetFile(string id)
        {
            var attachmentItem = await _storageService.GetFile(id);
            if (attachmentItem is null)
                return null;
            byte[] bytes = attachmentItem.Data;
            return new FileContentResult(bytes, attachmentItem.MimeType)
            {
                FileDownloadName = attachmentItem.Name
            };
        }

    }
}
