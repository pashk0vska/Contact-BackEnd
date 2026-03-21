using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Contact.API.Data;
using Contact.API.Models;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _context.Services.ToListAsync());

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var list = await _context.Services
                .Where(s => s.Category.ToLower() == category.ToLower())
                .ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var s = await _context.Services.FindAsync(id);
            if (s == null) return NotFound();
            return Ok(s);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(Service service)
        {
            if (string.IsNullOrWhiteSpace(service.Name))
                return BadRequest("Name is required.");
            if (service.Category != "Repair" && service.Category != "Sales")
                return BadRequest("Category must be 'Repair' or 'Sales'.");

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = service.Id }, service);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, Service updated)
        {
            var existing = await _context.Services.FindAsync(id);
            if (existing == null) return NotFound();
            if (string.IsNullOrWhiteSpace(updated.Name))
                return BadRequest("Name is required.");
            if (updated.Category != "Repair" && updated.Category != "Sales")
                return BadRequest("Category must be 'Repair' or 'Sales'.");

            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.Price = updated.Price;
            existing.Category = updated.Category;
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _context.Services.FindAsync(id);
            if (s == null) return NotFound();
            _context.Services.Remove(s);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}