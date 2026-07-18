using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Product : CompanyOwnedAuditableEntity
{
    private Product()
    {
    }

    public Product(
        Guid companyId,
        string internalCode,
        string name,
        Guid unitOfMeasureId,
        ProductType productType)
        : base(companyId)
    {
        SetInternalCode(internalCode);
        SetName(name);
        SetUnitOfMeasure(unitOfMeasureId);
        ProductType = productType;
        ControlsStock = productType is not (ProductType.ThirdParty or ProductType.Service);
        IsActive = true;
    }

    public string InternalCode { get; private set; } = string.Empty;

    public string? Sku { get; private set; }

    public string? Barcode { get; private set; }

    public string? Reference { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? ShortDescription { get; private set; }

    public string? Description { get; private set; }

    public ProductType ProductType { get; private set; }

    public bool ControlsStock { get; private set; }

    public bool IsActive { get; private set; } = true;

    public Guid? CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public Guid? SubcategoryId { get; private set; }

    public Subcategory? Subcategory { get; private set; }

    public Guid? BrandId { get; private set; }

    public Brand? Brand { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }

    public UnitOfMeasure? UnitOfMeasure { get; private set; }

    public Guid? ProductGroupId { get; private set; }

    public ProductGroup? ProductGroup { get; private set; }

    public Guid? MainSupplierId { get; private set; }

    public Supplier? MainSupplier { get; private set; }

    public Guid? PartnerId { get; private set; }

    public CommercialPartner? Partner { get; private set; }

    public Guid? DefaultWarehouseId { get; private set; }

    public Warehouse? DefaultWarehouse { get; private set; }

    public Guid? DefaultWarehouseLocationId { get; private set; }

    public InternalLocation? DefaultWarehouseLocation { get; private set; }

    public string? Ncm { get; private set; }

    public string? Cest { get; private set; }

    public string? ImagePath { get; private set; }

    public decimal CostPrice { get; private set; }

    public decimal SalePrice { get; private set; }

    public CommissionType? CommissionType { get; private set; }

    public decimal? CommissionValue { get; private set; }

    public DateOnly? PriceValidUntil { get; private set; }

    public decimal? MinimumStock { get; private set; }

    public string? Notes { get; private set; }

    public void Update(
        string internalCode,
        string? sku,
        string? barcode,
        string? reference,
        string name,
        string? shortDescription,
        string? description,
        ProductType productType,
        bool controlsStock,
        bool isActive,
        Guid? categoryId,
        Guid? subcategoryId,
        Guid? brandId,
        Guid unitOfMeasureId,
        Guid? productGroupId,
        Guid? mainSupplierId,
        Guid? partnerId,
        Guid? defaultWarehouseId,
        Guid? defaultWarehouseLocationId,
        string? ncm,
        string? cest,
        decimal costPrice,
        decimal salePrice,
        CommissionType? commissionType,
        decimal? commissionValue,
        DateOnly? priceValidUntil,
        decimal? minimumStock,
        string? notes)
    {
        SetInternalCode(internalCode);
        Sku = NormalizeCode(sku);
        Barcode = NormalizeDigits(barcode);
        Reference = NormalizeOptional(reference);
        SetName(name);
        ShortDescription = NormalizeOptional(shortDescription);
        Description = NormalizeOptional(description);
        ProductType = productType;
        SetUnitOfMeasure(unitOfMeasureId);
        CategoryId = categoryId;
        SubcategoryId = subcategoryId;
        BrandId = brandId;
        ProductGroupId = productGroupId;
        MainSupplierId = mainSupplierId;
        PartnerId = partnerId;
        SetFiscalCodes(ncm, cest);
        SetPrices(costPrice, salePrice);
        SetCommission(commissionType, commissionValue);
        PriceValidUntil = priceValidUntil;
        Notes = NormalizeOptional(notes);
        IsActive = isActive;
        SetStockPolicy(controlsStock, minimumStock, defaultWarehouseId, defaultWarehouseLocationId);
        EnforceTypeRules();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetImagePath(string? imagePath)
    {
        ImagePath = NormalizeOptional(imagePath);
    }

    public void UpdateInlinePrice(decimal? costPrice, decimal? salePrice)
    {
        if (costPrice is not null)
        {
            SetCostPrice(costPrice.Value);
        }

        if (salePrice is not null)
        {
            SetSalePrice(salePrice.Value);
        }
    }

    public void UpdateInlineMinimumStock(decimal? minimumStock)
    {
        if (!ControlsStock)
        {
            MinimumStock = null;
            return;
        }

        SetMinimumStock(minimumStock);
    }

    public void UpdateInlinePriceValidity(DateOnly? priceValidUntil)
    {
        PriceValidUntil = priceValidUntil;
    }

    public void UpdateInlineClassification(Guid? categoryId, Guid? brandId, Guid? supplierId)
    {
        if (categoryId.HasValue) CategoryId = categoryId;
        if (brandId.HasValue) BrandId = brandId;
        if (supplierId.HasValue) MainSupplierId = supplierId;
    }

    public void UpdateInlineCommission(CommissionType? commissionType, decimal? commissionValue)
    {
        SetCommission(commissionType, commissionValue);
    }

    public decimal CalculateMarginPercentage()
    {
        if (SalePrice <= 0)
        {
            return 0;
        }

        return Math.Round(((SalePrice - CostPrice) / SalePrice) * 100, 2);
    }

    private void SetInternalCode(string internalCode)
    {
        if (string.IsNullOrWhiteSpace(internalCode))
        {
            throw new ArgumentException("O código interno é obrigatório.", nameof(internalCode));
        }

        InternalCode = internalCode.Trim().ToUpperInvariant();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A descrição principal é obrigatória.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetUnitOfMeasure(Guid unitOfMeasureId)
    {
        if (unitOfMeasureId == Guid.Empty)
        {
            throw new ArgumentException("A unidade de medida é obrigatória.", nameof(unitOfMeasureId));
        }

        UnitOfMeasureId = unitOfMeasureId;
    }

    private void SetFiscalCodes(string? ncm, string? cest)
    {
        Ncm = NormalizeDigits(ncm);
        Cest = NormalizeDigits(cest);

        if (Ncm is not null && Ncm.Length != 8)
        {
            throw new ArgumentException("O NCM deve possuir exatamente 8 dígitos.", nameof(ncm));
        }

        if (Cest is not null && Cest.Length != 7)
        {
            throw new ArgumentException("O CEST deve possuir exatamente 7 dígitos.", nameof(cest));
        }
    }

    private void SetPrices(decimal costPrice, decimal salePrice)
    {
        SetCostPrice(costPrice);
        SetSalePrice(salePrice);
    }

    private void SetCostPrice(decimal costPrice)
    {
        if (costPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costPrice), "O preço de custo não pode ser negativo.");
        }

        CostPrice = costPrice;
    }

    private void SetSalePrice(decimal salePrice)
    {
        if (salePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(salePrice), "O preço de venda não pode ser negativo.");
        }

        SalePrice = salePrice;
    }

    private void SetCommission(CommissionType? commissionType, decimal? commissionValue)
    {
        if (commissionValue is null)
        {
            CommissionType = null;
            CommissionValue = null;
            return;
        }

        if (commissionValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(commissionValue), "A comissão não pode ser negativa.");
        }

        if (commissionType == Enums.CommissionType.Percentage && commissionValue > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(commissionValue), "A comissão percentual não pode ser superior a 100%.");
        }

        CommissionType = commissionType ?? Enums.CommissionType.Percentage;
        CommissionValue = commissionValue;
    }

    private void SetStockPolicy(
        bool controlsStock,
        decimal? minimumStock,
        Guid? defaultWarehouseId,
        Guid? defaultWarehouseLocationId)
    {
        ControlsStock = controlsStock;

        if (!controlsStock)
        {
            MinimumStock = null;
            DefaultWarehouseId = null;
            DefaultWarehouseLocationId = null;
            return;
        }

        SetMinimumStock(minimumStock);
        DefaultWarehouseId = defaultWarehouseId;
        DefaultWarehouseLocationId = defaultWarehouseLocationId;
    }

    private void SetMinimumStock(decimal? minimumStock)
    {
        if (minimumStock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumStock), "O estoque mínimo não pode ser negativo.");
        }

        MinimumStock = minimumStock;
    }

    private void EnforceTypeRules()
    {
        if (ProductType is ProductType.ThirdParty)
        {
            ControlsStock = false;
            MinimumStock = null;
            DefaultWarehouseId = null;
            DefaultWarehouseLocationId = null;

            if (PartnerId is null)
            {
                throw new InvalidOperationException("Produto de terceiro deve possuir parceiro comercial.");
            }
        }

        if (ProductType is ProductType.Service)
        {
            ControlsStock = false;
            MinimumStock = null;
            DefaultWarehouseId = null;
            DefaultWarehouseLocationId = null;
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}
