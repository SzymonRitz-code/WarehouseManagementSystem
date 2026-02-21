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
        public static void AddWmsMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.AddAuditLogMappings();
            cfg.AddStockMappings();
            cfg.AddStockReservationMappings();
            cfg.AddProductMappings();
            cfg.AddProductBatchMappings();
            cfg.AddDocumentMappings();
            cfg.AddDocumentItemMappings();
            cfg.AddWarehouseMappings();
            cfg.AddWarehouseZoneMappings();
        }

        public static void AddAuditLogMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<AuditLog, AuditLogDto>()
                .ForMember(dto => dto.PerformedByName, opt => opt.MapFrom(a => a.PerformedBy.Name))
                .ForMember(dto => dto.PerformedByEmail, opt => opt.MapFrom(a => a.PerformedBy.Email))
                .ReverseMap();
        }

        public static void AddStockMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Stock, StockDto>()
                .ForMember(dto => dto.ProductBatchNumber, opt => opt.MapFrom(s => s.ProductBatch != null ? s.ProductBatch.BatchNumber : null))
                .ForMember(dto => dto.ProductName, opt => opt.MapFrom(s => s.Product.Name))
                .ForMember(dto => dto.WarehouseName, opt => opt.MapFrom(s => s.Warehouse.Name))
                .ForMember(dto => dto.WarehouseZoneName, opt => opt.MapFrom(s => s.WarehouseZone.Name))
                .ReverseMap();
        }

        public static void AddStockReservationMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<StockReservation, StockReservationDto>().ReverseMap();
        }

        public static void AddProductMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Product, ProductDto>().ReverseMap();
        }

        public static void AddProductBatchMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ProductBatch, ProductBatchDto>()
                .ForMember(dto => dto.ProductName, opt => opt.MapFrom(pb => pb.Product.Name))
                .ReverseMap();
        }

        public static void AddDocumentMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Document, DocumentDto>()
                .ForMember(dto => dto.CreatedByName, opt => opt.MapFrom(d => d.CreatedBy.Name))
                .ForMember(dto => dto.CreatedByEmail, opt => opt.MapFrom(d => d.CreatedBy.Email))
                .ForMember(dto => dto.ConfirmedByName, opt => opt.MapFrom(d => d.ConfirmedBy != null ? d.ConfirmedBy.Name : null))
                .ForMember(dto => dto.ConfirmedByEmail, opt => opt.MapFrom(d => d.ConfirmedBy != null ? d.ConfirmedBy.Email : null))
                .ReverseMap();
        }

        public static void AddDocumentItemMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<DocumentItem, DocumentItemDto>()
                .ForMember(dto => dto.ProductName, opt => opt.MapFrom(di => di.Product.Name))
                .ForMember(dto => dto.ProductBatchNumber, opt => opt.MapFrom(di => di.ProductBatch != null ? di.ProductBatch.BatchNumber : null))
                .ForMember(dto => dto.SourceZoneName, opt => opt.MapFrom(di => di.SourceZone != null ? di.SourceZone.Name : null))
                .ForMember(dto => dto.TargetZoneName, opt => opt.MapFrom(di => di.TargetZone != null ? di.TargetZone.Name : null))
                .ReverseMap();
        }

        public static void AddWarehouseMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Warehouse, WarehouseDto>().ReverseMap();
        }

        public static void AddWarehouseZoneMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<WarehouseZone, WarehouseZoneDto>()
                .ForMember(dto => dto.WarehouseName, opt => opt.MapFrom(wz => wz.Warehouse.Name))
                .ReverseMap();
        }
    }
}
