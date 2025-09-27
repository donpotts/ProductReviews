using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AppProduct.Shared.Models;

namespace AppProduct.Services;

public interface IPdfGenerationService
{
    byte[] GenerateOrderPdf(Order order);
}

public class PdfGenerationService : IPdfGenerationService
{
    public byte[] GenerateOrderPdf(Order order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header()
                    .Text($"Order #{order.OrderNumber}")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);

                        // Order Information Section
                        x.Item().Element(OrderInfo);

                        // Items Section
                        x.Item().Element(ItemsTable);

                        // Totals Section
                        x.Item().Element(TotalsSection);

                        // Addresses Section
                        x.Item().Element(AddressesSection);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated on ");
                        x.Span($"{DateTime.Now:MMM dd, yyyy 'at' HH:mm}").SemiBold();
                        x.Span(" | ProductReviews Order System");
                    });
            });
        });

        return document.GeneratePdf();

        void OrderInfo(IContainer container)
        {
            container.Background(Colors.Grey.Lighten3)
                .Padding(20)
                .Column(column =>
                {
                    column.Spacing(5);
                    
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Order Information").SemiBold().FontSize(14);
                        row.ConstantItem(100).Text($"Status: {order.Status}").FontColor(GetStatusColor(order.Status));
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Order Date: {order.OrderDate:MMM dd, yyyy}");
                        row.RelativeItem().Text($"Payment: {GetPaymentMethodText(order.PaymentMethod)}");
                    });

                    if (order.EstimatedDeliveryDate.HasValue)
                    {
                        column.Item().Text($"Expected Delivery: {order.EstimatedDeliveryDate:MMM dd, yyyy}");
                    }
                });
        }

        void ItemsTable(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Items Ordered").SemiBold().FontSize(14).FontColor(Colors.Blue.Medium);
                
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Product name
                        columns.ConstantColumn(60); // Quantity
                        columns.ConstantColumn(80); // Unit price
                        columns.ConstantColumn(80); // Total
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Product").SemiBold();
                        header.Cell().Element(CellStyle).AlignCenter().Text("Qty").SemiBold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Price").SemiBold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Total").SemiBold();
                    });

                    foreach (var item in order.Items ?? new List<OrderItem>())
                    {
                        var displayName = item.Product?.GetDisplayName();
                        var productName = string.IsNullOrWhiteSpace(displayName) ? item.Product?.Name ?? "Unknown Product" : displayName;
                        table.Cell().Element(CellStyle).Text(productName);
                        table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString());
                        table.Cell().Element(CellStyle).AlignRight().Text($"${item.UnitPrice:F2}");
                        table.Cell().Element(CellStyle).AlignRight().Text($"${item.UnitPrice * item.Quantity:F2}");
                    }
                });
            });
        }

        void TotalsSection(IContainer container)
        {
            container.AlignRight().Width(200).Column(column =>
            {
                column.Spacing(5);
                
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Subtotal:");
                    row.ConstantItem(80).AlignRight().Text($"${order.Subtotal:F2}");
                });

                if (order.TaxAmount > 0)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Tax:");
                        row.ConstantItem(80).AlignRight().Text($"${order.TaxAmount:F2}");
                    });
                }

                if (order.ShippingAmount > 0)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Shipping:");
                        row.ConstantItem(80).AlignRight().Text($"${order.ShippingAmount:F2}");
                    });
                }

                column.Item().LineHorizontal(1).LineColor(Colors.Black);
                
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total:").SemiBold().FontSize(12);
                    row.ConstantItem(80).AlignRight().Text($"${order.TotalAmount:F2}").SemiBold().FontSize(12);
                });
            });
        }

        void AddressesSection(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Shipping Address").SemiBold().FontSize(12).FontColor(Colors.Blue.Medium);
                    column.Item().PaddingTop(5).Text(order.ShippingAddress ?? "Not provided");
                });

                row.ConstantItem(20); // Spacing between columns

                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Billing Address").SemiBold().FontSize(12).FontColor(Colors.Blue.Medium);
                    column.Item().PaddingTop(5).Text(order.BillingAddress ?? "Not provided");
                });
            });
        }

        static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
        }

        static string GetPaymentMethodText(PaymentMethod? paymentMethod)
        {
            return paymentMethod switch
            {
                PaymentMethod.CreditCard => "Credit Card",
                PaymentMethod.PurchaseOrder => "Purchase Order",
                PaymentMethod.Cash => "Cash",
                _ => "Unknown"
            };
        }

        static string GetStatusColor(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => Colors.Orange.Medium.ToString(),
                OrderStatus.Confirmed => Colors.Blue.Medium.ToString(),
                OrderStatus.Processing => Colors.Purple.Medium.ToString(),
                OrderStatus.Shipped => Colors.Green.Medium.ToString(),
                OrderStatus.Delivered => Colors.Green.Darken1.ToString(),
                OrderStatus.Cancelled => Colors.Red.Medium.ToString(),
                _ => Colors.Grey.Medium.ToString()
            };
        }
    }
}