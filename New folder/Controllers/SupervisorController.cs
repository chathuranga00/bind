using BlindMatchAPI.Data;
using BlindMatchAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlindMatchAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupervisorController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public SupervisorController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //Manual token validation helper
        private ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

                var handler = new JwtSecurityTokenHandler();
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                return handler.ValidateToken(token, parameters, out _);
            }
            catch
            {
                return null;
            }
        }

        //Helper: Extract supervisor ID from Authorization header
        private string? GetSupervisorId()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader.Substring(7);
            var principal = ValidateToken(token);
            return principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        //GET: api/supervisor/research-areas
        [HttpGet("research-areas")]
        public async Task<IActionResult> GetAllResearchAreas()
        {
            var supervisorId = GetSupervisorId();
            if (supervisorId == null)
                return Unauthorized("Invalid token.");

            var areas = await _context.ResearchAreas
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();
            return Ok(areas);
        }

        //GET: api/supervisor/preferences
        [HttpGet("preferences")]
        public async Task<IActionResult> GetMyPreferences()
        {
            var supervisorId = GetSupervisorId();
            if (supervisorId == null)
                return Unauthorized("Invalid token.");

            var preferences = await _context.SupervisorPreferences
                .Where(sp => sp.SupervisorId == supervisorId)
                .Select(sp => new
                {
                    sp.Id,
                    sp.ResearchAreaId,
                    ResearchAreaName = sp.ResearchArea.Name
                })
                .ToListAsync();
            return Ok(preferences);
        }

        //POST: api/supervisor/preferences/{researchAreaId}
        [HttpPost("preferences/{researchAreaId}")]
        public async Task<IActionResult> AddPreference(int researchAreaId)
        {
            var supervisorId = GetSupervisorId();
            if (supervisorId == null)
                return Unauthorized("Invalid token.");

            var areaExists = await _context.ResearchAreas
                .AnyAsync(r => r.Id == researchAreaId);
            if (!areaExists)
                return NotFound("Research area not found.");

            var alreadyExists = await _context.SupervisorPreferences
                .AnyAsync(sp => sp.SupervisorId == supervisorId
                             && sp.ResearchAreaId == researchAreaId);
            if (alreadyExists)
                return BadRequest("You already selected this research area.");

            var preference = new SupervisorPreference
            {
                SupervisorId = supervisorId,
                ResearchAreaId = researchAreaId
            };

            _context.SupervisorPreferences.Add(preference);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                preference.Id,
                preference.ResearchAreaId,
                ResearchAreaName = (await _context.ResearchAreas
                    .FindAsync(researchAreaId))!.Name
            });
        }

        //DELETE: api/supervisor/preferences/{researchAreaId}
        [HttpDelete("preferences/{researchAreaId}")]
        public async Task<IActionResult> RemovePreference(int researchAreaId)
        {
            var supervisorId = GetSupervisorId();
            if (supervisorId == null)
                return Unauthorized("Invalid token.");

            var preference = await _context.SupervisorPreferences
                .FirstOrDefaultAsync(sp => sp.SupervisorId == supervisorId
                                        && sp.ResearchAreaId == researchAreaId);
            if (preference == null)
                return NotFound("Preference not found.");

            _context.SupervisorPreferences.Remove(preference);
            await _context.SaveChangesAsync();
            return Ok("Preference removed.");
        }

        //GET: api/supervisor/blind-review
        [HttpGet("blind-review")]
        public async Task<IActionResult> GetBlindReviewProposals()
        {
            var supervisorId = GetSupervisorId();
            if (supervisorId == null)
                return Unauthorized("Invalid token.");

            var preferredAreaIds = await _context.SupervisorPreferences
                .Where(sp => sp.SupervisorId == supervisorId)
                .Select(sp => sp.ResearchAreaId)
                .ToListAsync();

            if (!preferredAreaIds.Any())
                return Ok(new
                {
                    message = "No preferences set.",
                    proposals = new List<object>()
                });

            var proposals = await _context.Projects
                .Where(p => preferredAreaIds.Contains(p.ResearchAreaId)
                         && p.Status == "Pending"
                         && p.Match == null)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Abstract,
                    p.TechStack,
                    p.Status,
                    ResearchArea = p.ResearchArea.Name,
                    p.ResearchAreaId
                })
                .ToListAsync();

            return Ok(proposals);
        }

        //POST: api/supervisor/express-interest/{projectId}
        [HttpPost("express-interest/{projectId}")]
        public async Task<IActionResult> ExpressInterest(int projectId)
        {
            var supervisorId = GetSupervisorId();
            if (supervisorId == null)
                return Unauthorized("Invalid token.");

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId
                                       && p.Status == "Pending");
            if (project == null)
                return NotFound("Project not found or no longer available.");
            
            var existingMatch = await _context.Matches
                .AnyAsync(m => m.ProjectId == projectId && m.SupervisorId == supervisorId);
            
            if (existingMatch)
                return BadRequest("Already expressed interest in this project.");

            var match = new Match
            {
                ProjectId = projectId,
                SupervisorId = supervisorId,
                SupervisorInterested = true,
                StudentInterested = false,
                Status = "interested",
                IsRevealed = false,
                CreatedAt = DateTime.UtcNow
            
        };

            project.Status = "Under Review";
            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Interest expressed successfully.",
                matchId = match.Id
            });
        }

        //GET: api/supervisor/matches
        [HttpGet("matches")]
        public async Task<IActionResult> GetMyMatches()
        {
            var supervisorId = GetSupervisorId();
            if (supervisorId == null)
                return Unauthorized("Invalid token.");

            var matches = await _context.Matches
                .Where(m => m.SupervisorId == supervisorId)
                .Include(m => m.Project)
                .ThenInclude(p => p.ResearchArea)
                .Include(m => m.Project)
                .ThenInclude(p => p.Student)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.ProjectId,
                    m.Status,
                    m.IsRevealed,
                    m.CreatedAt,
                    project = new
                    {
                        m.Project.Title,
                        m.Project.Abstract,
                        m.Project.TechStack,
                        m.Project.Status,
                        ResearchArea = m.Project.ResearchArea.Name
                    },
                    student = m.IsRevealed
                        ? new
                        {
                            m.Project.Student.Id,
                            m.Project.Student.FullName,
                            m.Project.Student.Email
                        }
                        : null
                })
                .ToListAsync();

            return Ok(matches);
        }
        
        //POST: api/supervisor/confirm/{matchId}
        [HttpPost("confirm/{matchId}")]
        public async Task<IActionResult> ConfirmMatch(int matchId)
        {
            var supervisorId = GetSupervisorId();
            if (supervisorId == null)
                return Unauthorized("Invalid token.");

            var match = await _context.Matches
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == matchId && m.SupervisorId == supervisorId);

            if (match == null)
                return NotFound("Match not found.");

            if (!match.SupervisorInterested)
                return BadRequest("You must express interest first.");

            if (match.IsRevealed)
                return BadRequest("Match is already confirmed.");

            match.Status = "confirmed";
            match.IsRevealed = true;
            match.StudentInterested = true;

            if (match.Project != null)
            {
                match.Project.Status = "Matched";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Match confirmed and identity revealed.",
                matchId = match.Id,
                match.Status,
                match.IsRevealed
            });
        }
    }
}