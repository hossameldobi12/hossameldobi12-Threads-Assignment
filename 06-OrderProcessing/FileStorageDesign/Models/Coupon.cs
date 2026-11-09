namespace FileStorageDesign.Models;

public class Coupon
{
    public string Code { get; set; }
    public int DiscountPercent { get; set; }
    public DateTime ExpiryDate { get; set; }

    
}