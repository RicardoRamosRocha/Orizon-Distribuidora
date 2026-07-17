# Modelo de Domínio

As entidades de cadastros herdam auditoria e soft delete pelo padrão existente.

Entidades simples baseadas em `BasicRegistrationEntity`:

- `Category`
- `Brand`
- `ProductGroup`
- `Warehouse`
- `InternalLocation`
- `RegistrationStatus`
- `Subcategory`
- `UnitOfMeasure`

Entidades com campos próprios:

- `Supplier`
- `CommercialPartner`

Todas possuem `Guid` como identificador e `CompanyId` para isolamento multiempresa.

