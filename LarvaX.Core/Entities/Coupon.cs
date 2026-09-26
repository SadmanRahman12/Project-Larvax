using System;

namespace LarvaX.Core.Entities
{
    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public decimal DiscountPercent { get; set; } = 0;
        public decimal FixedDiscountAmount { get; set; } = 0;
        public DateTime? ValidUntil { get; set; }
        public bool IsActive { get; set; } = true;
        public int MaxUses { get; set; } = 1000;
        public int TimesUsed { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
