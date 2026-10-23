namespace RestaurantPos.Services;

public static class Pricing
{
    public static readonly string[] OrderTypes = ["DineIn", "Takeaway", "Delivery", "Online"];
    public static readonly string[] Courses = ["Starter", "Main", "Dessert", "Drink"];
    public static readonly string[] PayMethods = ["Cash", "Card", "Mobile", "Due"];
    public static readonly string[] PrepStates = ["Pending", "Preparing", "Ready", "Served"];

    public static string OrderType(string? value)
    {
        var match = OrderTypes.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
        return match ?? throw new AppException("Order type must be Dine-in, Takeaway, Delivery, or Online.");
    }

    public static string Course(string? value)
    {
        var match = Courses.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
        return match ?? "Main";
    }

    public static string Method(string? value)
    {
        var match = PayMethods.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
        return match ?? throw new AppException("Payment method must be Cash, Card, Mobile, or Due.");
    }

    public static void Apply(SaleOrder order, Branch branch, Coupon? coupon)
    {
        foreach (var line in order.Lines)
        {
            if (line.IsVoid)
            {
                line.LineTotal = 0;
                continue;
            }
            var extra = line.Extras.Sum(e => e.Price);
            var gross = (line.UnitPrice + extra) * line.Qty;
            var disc = Math.Min(Math.Max(line.DiscountAmount, 0), gross);
            line.DiscountAmount = Money.R(disc);
            line.LineTotal = Money.R(gross - disc);
        }

        decimal subtotal = 0, lineDisc = 0;
        foreach (var line in order.Lines.Where(l => !l.IsVoid))
        {
            var extra = line.Extras.Sum(e => e.Price);
            subtotal += (line.UnitPrice + extra) * line.Qty;
            lineDisc += line.DiscountAmount;
        }
        subtotal = Money.R(subtotal);
        lineDisc = Money.R(lineDisc);

        decimal couponDisc = 0;
        var couponOk = coupon is { IsActive: true } && (coupon.ExpiresOn == null || coupon.ExpiresOn >= DateTime.UtcNow);
        if (couponOk && coupon != null)
        {
            var baseAmt = Math.Max(0, subtotal - lineDisc);
            if (baseAmt + 0.001m >= coupon.MinOrder)
            {
                couponDisc = coupon.DiscountType == "Percent"
                    ? Money.R(baseAmt * coupon.Value / 100m)
                    : coupon.Value;
                if (coupon.MaxDiscount > 0) couponDisc = Math.Min(couponDisc, coupon.MaxDiscount);
                couponDisc = Math.Min(couponDisc, baseAmt);
            }
        }

        var room = Math.Max(0, subtotal - lineDisc - couponDisc);
        order.ManualDiscount = Money.R(Math.Min(Math.Max(order.ManualDiscount, 0), room));
        var discount = Money.R(lineDisc + couponDisc + order.ManualDiscount);
        var net = Math.Max(0, Money.R(subtotal - discount));
        var service = order.OrderType == "DineIn" ? Money.R(net * branch.ServiceChargePercent / 100m) : 0;
        var tax = Money.R((net + service) * branch.TaxPercent / 100m);
        order.Subtotal = subtotal;
        order.DiscountTotal = discount;
        order.ServiceCharge = service;
        order.TaxTotal = tax;
        order.GrandTotal = Money.R(net + service + tax);
        RefreshPayment(order);
    }

    public static void RefreshPayment(SaleOrder order)
    {
        if (order.Status == "Void")
        {
            order.PaymentStatus = "Void";
            return;
        }
        var due = Money.R(order.GrandTotal - order.PaidTotal);
        if (order.RefundedTotal > 0 && order.PaidTotal > 0 && order.RefundedTotal >= order.PaidTotal - 0.009m)
            order.PaymentStatus = "Refunded";
        else if (order.RefundedTotal > 0)
            order.PaymentStatus = due <= 0.009m ? "PartialRefund" : "Partial";
        else if (order.GrandTotal > 0 && due <= 0.009m)
            order.PaymentStatus = "Paid";
        else if (order.PaidTotal > 0)
            order.PaymentStatus = "Partial";
        else
            order.PaymentStatus = "Unpaid";

        if (order.PaymentStatus is "Paid" or "PartialRefund")
            order.PaidAt ??= DateTime.UtcNow;
    }

    public static void RefreshKitchen(SaleOrder order)
    {
        var lines = order.Lines.Where(l => !l.IsVoid).ToList();
        if (lines.Count == 0 || lines.All(l => !l.SentToKitchen)) order.KitchenStatus = "None";
        else if (lines.Where(l => l.SentToKitchen).All(l => l.PrepStatus == "Served")) order.KitchenStatus = "Served";
        else if (lines.Where(l => l.SentToKitchen).All(l => l.PrepStatus is "Ready" or "Served")) order.KitchenStatus = "Ready";
        else if (lines.Any(l => l.PrepStatus is "Preparing" or "Ready" or "Served")) order.KitchenStatus = "Preparing";
        else order.KitchenStatus = "Submitted";
    }
}
