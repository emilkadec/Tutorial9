using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;
    
    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }
    
    [HttpPost("add")]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductWarehouseModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        if (model.Amount <= 0)
        {
            return BadRequest("Amount must be greater than 0");
        }
        
        var result = await _dbService.AddProductToWarehouseAsync(model);
        
        if (result == null)
        {
            return NotFound("Requested warehouse, product, or order not found");
        }
        
        return Ok(new { IdProductWarehouse = result });
    }
    
    [HttpPost("addProc")]
    public async Task<IActionResult> AddProductToWarehouseUsingProc([FromBody] ProductWarehouseModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        if (model.Amount <= 0)
        {
            return BadRequest("Amount must be greater than 0");
        }
        
        var result = await _dbService.AddProductToWarehouseWithProcAsync(model);
        
        if (result == null)
        {
            return NotFound("Requested warehouse, product, or order not found");
        }
        
        return Ok(new { IdProductWarehouse = result });
    }
}