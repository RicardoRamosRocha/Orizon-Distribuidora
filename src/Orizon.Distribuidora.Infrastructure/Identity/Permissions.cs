namespace Orizon.Distribuidora.Infrastructure.Identity;

public static class Permissions
{
    public static class Companies
    {
        public const string View = "companies.view";
        public const string Manage = "companies.manage";
    }

    public static class BasicRegistrations
    {
        public const string View = "basic-registrations.view";
        public const string Create = "basic-registrations.create";
        public const string Edit = "basic-registrations.edit";
        public const string Delete = "basic-registrations.delete";
    }

    public static class Users
    {
        public const string View = "users.view";
        public const string Manage = "users.manage";
    }

    public static class Products
    {
        public const string View = "products.view";
        public const string Create = "products.create";
        public const string Edit = "products.edit";
        public const string Delete = "products.delete";
        public const string Import = "products.import";
    }

    public static class Inventory
    {
        public const string View = "inventory.view";
        public const string Adjust = "inventory.adjust";
    }

    public static class Sales
    {
        public const string View = "sales.view";
        public const string Create = "sales.create";
        public const string Cancel = "sales.cancel";
    }
}
