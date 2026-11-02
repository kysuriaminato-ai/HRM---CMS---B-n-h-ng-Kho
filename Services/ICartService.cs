using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartViewModelAsync(int branchId, int? userId, string sessionId);
        Task<CartOperationResultDto> AddToCartAsync(int productId, int quantity, int branchId, int? userId, string sessionId);
        Task<CartOperationResultDto> UpdateQuantityInstantAsync(int productId, int newQuantity, int branchId, int? userId, string sessionId);
        Task<CartOperationResultDto> RemoveItemAsync(int productId, int? userId, string sessionId);
        Task ClearCartAsync(int? userId, string sessionId);
        Task SyncSessionCartToDatabaseAsync(int userId, string sessionId);
    }
}
