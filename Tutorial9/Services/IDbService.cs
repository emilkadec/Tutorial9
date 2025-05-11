using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IDbService
{
    Task<int?> AddProductToWarehouseAsync(ProductWarehouseModel model);
    Task<int?> AddProductToWarehouseWithProcAsync(ProductWarehouseModel model);
}