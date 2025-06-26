using System.Security.Claims;
using WebThoiTrang.Models;

namespace WebThoiTrang.Repositories.Interface
{
    public interface ICheckoutService
    {
        Task<CheckoutViewModel> BuildViewModelAsync(ClaimsPrincipal user);
        Task<int> PlaceOrderAsync(CheckoutViewModel vm, ClaimsPrincipal user);
        Task<VoucherResult> ValidateVoucherAsync(string code);
    }

    public record VoucherResult(bool IsValid, decimal Discount, string Message);
}
