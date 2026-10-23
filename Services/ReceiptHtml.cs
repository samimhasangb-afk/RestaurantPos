using System.Net;
using System.Text;

namespace RestaurantPos.Services;

public static class ReceiptHtml
{
    public static string Build(SaleOrder order, Receipt receipt, int width, string verifyUrl, string qrUrl, string footer)
    {
        var narrow = width <= 58;
        var page = narrow ? "58mm" : "80mm";
        var body = narrow ? "52mm" : "72mm";
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>");
        sb.Append(WebUtility.HtmlEncode(receipt.ReceiptNo));
        sb.Append("</title><style>");
        sb.Append($"@page {{ size: {page} auto; margin: 3mm; }}");
        sb.Append("html,body{margin:0;background:#fff;color:#111;}");
        sb.Append($"body{{width:{body};font-family:Arial,sans-serif;font-size:12px;padding:2mm;}}");
        sb.Append("h1{font-size:16px;margin:0;text-align:center;} .c{text-align:center;} .m{margin:2px 0;}");
        sb.Append("table{width:100%;border-collapse:collapse;} td{vertical-align:top;padding:2px 0;} .r{text-align:right;}");
        sb.Append(".line{border-top:1px dashed #333;margin:6px 0;} .tot td{font-weight:700;} img{width:120px;height:120px;}");
        sb.Append("</style></head><body>");
        sb.Append("<h1>Saffron POS</h1>");
        sb.Append("<div class=\"c m\">").Append(WebUtility.HtmlEncode(order.Branch?.Name)).Append("</div>");
        if (!string.IsNullOrWhiteSpace(order.Branch?.Address))
            sb.Append("<div class=\"c m\">").Append(WebUtility.HtmlEncode(order.Branch.Address)).Append("</div>");
        if (!string.IsNullOrWhiteSpace(order.Branch?.Phone))
            sb.Append("<div class=\"c m\">").Append(WebUtility.HtmlEncode(order.Branch.Phone)).Append("</div>");
        sb.Append("<div class=\"line\"></div>");
        Row(sb, "Receipt", receipt.ReceiptNo);
        Row(sb, "Order", order.OrderNo);
        Row(sb, "Type", order.OrderType);
        if (!string.IsNullOrWhiteSpace(order.Table?.Name)) Row(sb, "Table", order.Table.Name);
        Row(sb, "Time", order.CreatedAt.ToLocalTime().ToString("dd MMM yyyy hh:mm tt"));
        if (!string.IsNullOrWhiteSpace(order.Cashier?.FullName)) Row(sb, "Cashier", order.Cashier.FullName);
        if (!string.IsNullOrWhiteSpace(order.Waiter?.FullName)) Row(sb, "Waiter", order.Waiter.FullName);
        if (!string.IsNullOrWhiteSpace(order.Customer?.Name)) Row(sb, "Customer", order.Customer.Name);
        sb.Append("<div class=\"line\"></div><table>");
        foreach (var line in order.Lines.Where(l => !l.IsVoid))
        {
            sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(line.Name));
            sb.Append("<div>").Append(line.Qty.ToString("0.###")).Append(" x ").Append(line.UnitPrice.ToString("0.00"));
            foreach (var extra in line.Extras)
                sb.Append("<br>+ ").Append(WebUtility.HtmlEncode(extra.Name));
            if (!string.IsNullOrWhiteSpace(line.Notes))
                sb.Append("<br><i>").Append(WebUtility.HtmlEncode(line.Notes)).Append("</i>");
            sb.Append("</div></td><td class=\"r\">").Append(line.LineTotal.ToString("0.00")).Append("</td></tr>");
        }
        sb.Append("</table><div class=\"line\"></div><table>");
        MoneyRow(sb, "Subtotal", order.Subtotal, false);
        MoneyRow(sb, "Discount", order.DiscountTotal, false);
        MoneyRow(sb, "Service", order.ServiceCharge, false);
        MoneyRow(sb, "Tax", order.TaxTotal, false);
        MoneyRow(sb, "Total", order.GrandTotal, true);
        MoneyRow(sb, "Paid", order.PaidTotal, false);
        if (order.RefundedTotal > 0) MoneyRow(sb, "Refunded", order.RefundedTotal, false);
        MoneyRow(sb, "Due", Money.R(order.GrandTotal - order.PaidTotal), true);
        sb.Append("</table>");
        foreach (var tender in order.Tenders)
        {
            var label = tender.IsRefund ? "Refund " + tender.Method : tender.Method;
            sb.Append("<div class=\"m\">").Append(WebUtility.HtmlEncode(label)).Append(": ").Append(tender.Amount.ToString("0.00"));
            if (!tender.IsRefund && tender.ChangeDue > 0)
                sb.Append(" (change ").Append(tender.ChangeDue.ToString("0.00")).Append(')');
            sb.Append("</div>");
        }
        sb.Append("<div class=\"line\"></div><div class=\"c\"><img alt=\"QR\" src=\"").Append(WebUtility.HtmlEncode(qrUrl)).Append("\"><div class=\"m\">Scan to verify</div><div class=\"m\">");
        sb.Append(WebUtility.HtmlEncode(verifyUrl)).Append("</div><div class=\"m\">").Append(WebUtility.HtmlEncode(footer)).Append("</div></div>");
        sb.Append("<script>window.onload=function(){setTimeout(function(){window.print()}, 250)}</script>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    static void Row(StringBuilder sb, string label, string value)
    {
        sb.Append("<div class=\"m\"><b>").Append(WebUtility.HtmlEncode(label)).Append(":</b> ").Append(WebUtility.HtmlEncode(value)).Append("</div>");
    }

    static void MoneyRow(StringBuilder sb, string label, decimal amount, bool strong)
    {
        sb.Append(strong ? "<tr class=\"tot\">" : "<tr>");
        sb.Append("<td>").Append(WebUtility.HtmlEncode(label)).Append("</td><td class=\"r\">").Append(amount.ToString("0.00")).Append("</td></tr>");
    }
}
