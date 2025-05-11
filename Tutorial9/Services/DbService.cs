using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<int?> AddProductToWarehouseAsync(ProductWarehouseModel model)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", model.IdProduct);
        
        var productExists = await command.ExecuteScalarAsync();
        if (productExists == null)
        {
            return null; 
        }
        
        command.Parameters.Clear();
        command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        command.Parameters.AddWithValue("@IdWarehouse", model.IdWarehouse);
        
        var warehouseExists = await command.ExecuteScalarAsync();
        if (warehouseExists == null)
        {
            return null; 
        }
        
        command.Parameters.Clear();
        command.CommandText = @"
            SELECT TOP 1 o.IdOrder, p.Price
            FROM ""Order"" o
            JOIN Product p ON o.IdProduct = p.IdProduct
            LEFT JOIN Product_Warehouse pw ON o.IdOrder = pw.IdOrder
            WHERE o.IdProduct = @IdProduct 
              AND o.Amount = @Amount 
              AND pw.IdProductWarehouse IS NULL 
              AND o.CreatedAt < @CreatedAt";
        
        command.Parameters.AddWithValue("@IdProduct", model.IdProduct);
        command.Parameters.AddWithValue("@Amount", model.Amount);
        command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);
        
        int? orderId = null;
        decimal price = 0;
        
        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                orderId = reader.GetInt32(0);
                price = reader.GetDecimal(1);
            }
        }
        
        if (orderId == null)
        {
            return null; 
        }
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;
        
        try
        {
            command.Parameters.Clear();
            command.CommandText = @"UPDATE ""Order"" SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            
            await command.ExecuteNonQueryAsync();
            
            command.Parameters.Clear();
            command.CommandText = @"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                SELECT SCOPE_IDENTITY();";
            
            command.Parameters.AddWithValue("@IdWarehouse", model.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", model.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            command.Parameters.AddWithValue("@Amount", model.Amount);
            command.Parameters.AddWithValue("@Price", price * model.Amount);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            
            var result = await command.ExecuteScalarAsync();
            
            await transaction.CommitAsync();
            
            return Convert.ToInt32(result);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<int?> AddProductToWarehouseWithProcAsync(ProductWarehouseModel model)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand("AddProductToWarehouse", connection);
        
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.AddWithValue("@IdProduct", model.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", model.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", model.Amount);
        command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);
        
        await connection.OpenAsync();
        
        try
        {
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? null : Convert.ToInt32(result);
        }
        catch (SqlException ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }
}