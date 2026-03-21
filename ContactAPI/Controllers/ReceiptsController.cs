using Microsoft.AspNetCore.Mvc;
using Contact.API.Data;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReceiptsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/receipts/{id}?type=sale або repair
        [HttpGet("{id}")]
        public IActionResult GetReceipt(int id, [FromQuery] string type)
        {
            if (type == "sale")
            {
                var sale = _context.SaleHeaders.Find(id);
                if (sale == null) return NotFound();
                return Ok(new
                {
                    Type = "Sale",
                    sale.Id,
                    sale.ClientId,
                    sale.Price,
                    sale.Date
                });
            }
            else if (type == "repair")
            {
                var repair = _context.Repairs.Find(id);
                if (repair == null) return NotFound();
                return Ok(new
                {
                    Type = "Repair",
                    repair.Id,
                    repair.ClientId,
                    repair.Model,
                    repair.TotalCost,
                    repair.Status
                });
            }
            return BadRequest("Invalid type parameter (use 'sale' or 'repair').");
        }
    }
}
