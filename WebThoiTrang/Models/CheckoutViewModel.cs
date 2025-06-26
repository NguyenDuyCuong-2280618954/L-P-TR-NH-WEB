namespace WebThoiTrang.Models
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ShippingInfo ShippingInfo { get; set; } = new ShippingInfo();
        public PaymentInfo PaymentInfo { get; set; } = new PaymentInfo();
        public decimal SubTotal { get; set; }
        public decimal GiftWrapFee { get; set; } = 0;
        public decimal ShippingFee { get; set; }
        public decimal Total => SubTotal + ShippingFee + GiftWrapFee;

        public bool IsGiftWrap { get; set; }
        
        public string VoucherCode { get; set; } = string.Empty;
        
    }
}
