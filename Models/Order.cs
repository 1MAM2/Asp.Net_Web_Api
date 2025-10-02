using System;
using System.Security.Cryptography;

public class Order
{
    public int Id { get; set; } // 6 haneli int ID
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal TotalPrice { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public Order()
    {
        Id = GenerateRandomId();
    }

    private int GenerateRandomId()
    {
        // 100000 - 999999 arası rastgele int üret
        using var rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[4];
        int value;

        do
        {
            rng.GetBytes(bytes);
            value = BitConverter.ToInt32(bytes, 0);
            value = Math.Abs(value % 1000000); // 0 - 999999 arası
        } while (value < 100000); // 6 haneli olana kadar tekrar et

        return value;
    }
    public enum OrderStatus
    {
        Cancelled = 0,    // İptal edildi
        Pending = 1,     // Sipariş alındı ama işleme başlanmadı
        Processing = 2,  // Hazırlanıyor
        Shipped = 3,     // Kargoya verildi
        Delivered = 4,   // Teslim edildi
    }
}