using Microsoft.AspNetCore.Mvc;
using Contact.API.Data;
using Contact.API.Models;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/services
        [HttpGet]
        public IActionResult GetAll() => Ok(_context.Services.ToList());

        // GET: api/services/category/Repair  або  /Sales
        [HttpGet("category/{category}")]
        public IActionResult GetByCategory(string category)
        {
            var list = _context.Services
                .Where(s => s.Category.ToLower() == category.ToLower())
                .ToList();
            return Ok(list);
        }

        // GET: api/services/5
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var s = _context.Services.Find(id);
            if (s == null) return NotFound();
            return Ok(s);
        }

        // POST: api/services
        [HttpPost]
        public IActionResult Create(Service service)
        {
            // Мінімальна валідація
            if (string.IsNullOrWhiteSpace(service.Name)) return BadRequest("Name is required.");
            if (service.Category != "Repair" && service.Category != "Sales") return BadRequest("Category must be 'Repair' or 'Sales'.");

            _context.Services.Add(service);
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetById), new { id = service.Id }, service);
        }

        // PUT: api/services/5
        [HttpPut("{id}")]
        public IActionResult Update(int id, Service updated)
        {
            var existing = _context.Services.Find(id);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(updated.Name)) return BadRequest("Name is required.");
            if (updated.Category != "Repair" && updated.Category != "Sales") return BadRequest("Category must be 'Repair' or 'Sales'.");

            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.Price = updated.Price;
            existing.Category = updated.Category;

            _context.SaveChanges();
            return Ok(existing);
        }

        // DELETE: api/services/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var s = _context.Services.Find(id);
            if (s == null) return NotFound();

            _context.Services.Remove(s);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
