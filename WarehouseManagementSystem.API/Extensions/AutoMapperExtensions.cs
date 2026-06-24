namespace WarehouseManagementSystem.API.Extensions
{
    using AutoMapper;
    using WarehouseManagementSystem.API.DTO;
    using WarehouseManagementSystem.Domain.Model.AuditDomain;
    using WarehouseManagementSystem.Domain.Model.CatalogDomain;
    using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
    using WarehouseManagementSystem.Domain.Model.InventoryDomain;
    using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

    public static class AutoMapperExtensions
    {
        /// <summary>
        /// Adds all the necessary mappings for the WMS application to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddWmsMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.AddAuditLogMappings();
            cfg.AddProductMappings();
            cfg.AddWarehouseMappings();
            cfg.AddWarehouseZoneMappings();
            cfg.AddStockMappings();
            cfg.AddStockReservationMappings();
            cfg.AddProductBatchMappings();
            cfg.AddDocumentMappings();
            cfg.AddDocumentItemMappings();

        }
        /// <summary>
        /// Adds the mappings for the AuditLog entity to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddAuditLogMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<AuditLog, AuditLogDto>()
                .ForMember(dto => dto.PerformedByName, opt => opt.MapFrom(_ => string.Empty))
                .ForMember(dto => dto.PerformedByEmail, opt => opt.MapFrom(_ => string.Empty));
        }
        /// <summary>
        /// Adds the mappings for the Stock entity to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddStockMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Stock, StockDto>()
                .ForMember(dto => dto.ProductBatchNumber, opt => opt.MapFrom(s => s.ProductBatch != null ? s.ProductBatch.BatchNumber : null))
                .ForMember(dto => dto.ProductName, opt => opt.MapFrom(s => s.Product.Name))
                .ForMember(dto => dto.WarehouseName, opt => opt.MapFrom(s => s.Warehouse.Name))
                .ForMember(dto => dto.ZoneName, opt => opt.MapFrom(s => s.WarehouseZone.Name));
        }
        /// <summary>
        /// Adds the mappings for the StockReservation entity to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddStockReservationMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<StockReservation, StockReservationDto>();
        }
        /// <summary>
        /// Adds the mappings for the Product entity to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddProductMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Product, ProductDetailsDto>()
                .ForMember(dto => dto.Sku, opt => opt.MapFrom(p => p.SKU));
        }
        /// <summary>
        /// Adds the mappings for the Warehouse entity to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddWarehouseMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Warehouse, WarehouseDetailsDto>();
        }
        /// <summary>
        /// Adds the mappings for the WarehouseZone entity to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddWarehouseZoneMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<WarehouseZone, WarehouseZoneDetailsDto>()
                .ForMember(dto => dto.WarehouseName, opt => opt.MapFrom(z => z.Warehouse != null ? z.Warehouse.Name : null));
        }
        /// <summary>
        /// Adds the mappings for the ProductBatch entity to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddProductBatchMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ProductBatch, ProductBatchDto>()
                .ForMember(dto => dto.ProductName, opt => opt.MapFrom(pb => pb.Product.Name));
        }
        /// <summary>
        /// Adds the mappings for the Document entity to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddDocumentMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Document, DocumentDto>()
                .ForMember(dto => dto.SourceWarehouseName, opt => opt.MapFrom(d => d.SourceWarehouse != null ? d.SourceWarehouse.Name : null))
                .ForMember(dto => dto.TargetWarehouseName, opt => opt.MapFrom(d => d.TargetWarehouse != null ? d.TargetWarehouse.Name : null))
                .ForMember(dto => dto.CreatedById, opt => opt.MapFrom(d => d.CreatedByUser != null ? (Guid?)d.CreatedByUser.Id : null))
                .ForMember(dto => dto.CreatedByEmail, opt => opt.MapFrom(d => d.CreatedByUser != null ? d.CreatedByUser.Email : null))
                .ForMember(dto => dto.CreatedByName, opt => opt.MapFrom(d => d.CreatedByUser != null ? d.CreatedByUser.Name : null))
                .ForMember(dto => dto.ConfirmedById, opt => opt.MapFrom(d => d.ConfirmedByUser != null ? (Guid?)d.ConfirmedByUser.Id : null))
                .ForMember(dto => dto.ConfirmedByEmail, opt => opt.MapFrom(d => d.ConfirmedByUser != null ? d.ConfirmedByUser.Email : null))
                .ForMember(dto => dto.ConfirmedByName, opt => opt.MapFrom(d => d.ConfirmedByUser != null ? d.ConfirmedByUser.Name : null));
        }
        /// <summary>
        /// Adds the mappings for the DocumentItem entity to the AutoMapper configuration.
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddDocumentItemMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<DocumentItem, DocumentItemDto>()
                .ForMember(dto => dto.ProductName, opt => opt.MapFrom(di => di.Product.Name))
                .ForMember(dto => dto.ProductBatchNumber, opt => opt.MapFrom(di => di.ProductBatch != null ? di.ProductBatch.BatchNumber : null))
                .ForMember(dto => dto.SourceZoneName, opt => opt.MapFrom(di => di.SourceZone != null ? di.SourceZone.Name : null))
                .ForMember(dto => dto.TargetZoneName, opt => opt.MapFrom(di => di.TargetZone != null ? di.TargetZone.Name : null));
        }

    }
}
