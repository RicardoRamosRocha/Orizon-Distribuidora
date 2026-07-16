namespace Orizon.Distribuidora.Infrastructure.Identity;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Seller = "Seller";
    public const string Buyer = "Buyer";
    public const string Financial = "Financial";
    public const string Stockist = "Stockist";

    public static readonly string[] All =
    [
        Administrator,
        Manager,
        Seller,
        Buyer,
        Financial,
        Stockist
    ];
}
