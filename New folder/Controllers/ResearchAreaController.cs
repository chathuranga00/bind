using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlindMatchAPI.Data;
using BlindMatchAPI.Models;

namespace BlindMatchAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResearchAreaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResearchAreaController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResearchArea>>> GetResearchAreas()
        {
            
            return await _context.ResearchAreas
                .OrderByDescending(a => a.Id) 
                .ToListAsync();
        }

       
        [HttpPost]
        public async Task<ActionResult<ResearchArea>> PostResearchArea(ResearchArea researchArea)
        {
            if (researchArea == null)
            {
                return BadRequest("Invalid research area data.");
            }

            
            researchArea.Projects = new List<Project>();
            researchArea.SupervisorPreferences = new List<SupervisorPreference>();

            _context.ResearchAreas.Add(researchArea);
            await _context.SaveChangesAsync();

            
            return Ok(researchArea);
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResearchArea(int id)
        {
            var area = await _context.ResearchAreas.FindAsync(id);
            if (area == null)
            {
                return NotFound();
            }

            _context.ResearchAreas.Remove(area);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Research area deleted successfully!" });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> PutResearchArea(int id, [FromBody] ResearchArea researchArea)
        {
            
            if (id != researchArea.Id)
            {
                return BadRequest("ID mismatch");
            }

            
            var existingArea = await _context.ResearchAreas.FindAsync(id);
            if (existingArea == null)
            {
                return NotFound();
            }

            
            existingArea.Name = researchArea.Name;
            existingArea.Description = researchArea.Description;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Updated successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}