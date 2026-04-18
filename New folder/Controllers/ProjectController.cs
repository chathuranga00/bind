using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlindMatchAPI.Data;
using BlindMatchAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlindMatchAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public ProjectController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

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

        private string? GetUserIdFromToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader.Substring(7);
            var principal = ValidateToken(token);
            return principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            var projects = await (from p in _context.Projects
                                  join ra in _context.ResearchAreas on p.ResearchAreaId equals ra.Id
                                  join u in _context.Users on p.StudentId equals u.Id
                                  
                                  join m in _context.Matches on p.Id equals m.ProjectId into pm
                                  from match in pm.DefaultIfEmpty()
                                  join sup in _context.Users on match.SupervisorId equals sup.Id into supm
                                  from supervisor in supm.DefaultIfEmpty()
                                  select new
                                  {
                                      id = p.Id,
                                      code = "PAS-2026-" + p.Id.ToString("D3"),
                                      title = p.Title,
                                      studentName = u.FullName, 
                                      area = ra.Name,
                                      status = p.Status,
                                      
                                      supervisor = supervisor != null ? supervisor.FullName : "—"
                                  }).ToListAsync();

            return Ok(projects);
        }

        [HttpGet("my-proposals")]
        public async Task<IActionResult> GetMyProposals()
        {
            var studentId = GetUserIdFromToken();
            if (studentId == null)
                return Unauthorized("Invalid token.");

            var proposals = await _context.Projects
                .Where(p => p.StudentId == studentId)
                .Include(p => p.ResearchArea)
                .Include(p => p.Match)
                .ThenInclude(m => m.Supervisor)
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    code = "PAS-2026-" + p.Id.ToString("D3"),
                    p.Title,
                    p.Abstract,
                    p.TechStack,
                    p.Status,
                    p.ResearchAreaId,
                    researchArea = p.ResearchArea.Name,
                    supervisor = p.Match != null && p.Match.IsRevealed
                        ? new
                        {
                            p.Match.Supervisor.Id,
                            p.Match.Supervisor.FullName,
                            p.Match.Supervisor.Email
                        }
                        : null
                })
                .ToListAsync();

            return Ok(proposals);
        }

        [HttpGet("my-proposals/{id}")]
        public async Task<IActionResult> GetMyProposalById(int id)
        {
            var studentId = GetUserIdFromToken();
            if (studentId == null)
                return Unauthorized("Invalid token.");

            var proposal = await _context.Projects
                .Where(p => p.Id == id && p.StudentId == studentId)
                .Include(p => p.ResearchArea)
                .Include(p => p.Match)
                .ThenInclude(m => m.Supervisor)
                .Select(p => new
                {
                    p.Id,
                    code = "PAS-2026-" + p.Id.ToString("D3"),
                    p.Title,
                    p.Abstract,
                    p.TechStack,
                    p.Status,
                    p.ResearchAreaId,
                    researchArea = p.ResearchArea.Name,
                    canEdit = p.Status != "Matched" && p.Status != "Withdrawn",
                    canWithdraw = p.Status != "Matched" && p.Status != "Withdrawn",
                    supervisor = p.Match != null && p.Match.IsRevealed
                        ? new
                        {
                            p.Match.Supervisor.Id,
                            p.Match.Supervisor.FullName,
                            p.Match.Supervisor.Email
                        }
                        : null
                })
                .FirstOrDefaultAsync();

            if (proposal == null)
                return NotFound("Proposal not found.");

            return Ok(proposal);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProposal([FromBody] CreateProposalRequest request)
        {
            var studentId = GetUserIdFromToken();
            if (studentId == null)
                return Unauthorized("Invalid token.");

            if (string.IsNullOrWhiteSpace(request.Title) ||
                string.IsNullOrWhiteSpace(request.Abstract) ||
                string.IsNullOrWhiteSpace(request.TechStack))
            {
                return BadRequest("All fields are required.");
            }

            var areaExists = await _context.ResearchAreas.AnyAsync(r => r.Id == request.ResearchAreaId);
            if (!areaExists)
                return BadRequest("Invalid research area.");

            var project = new Project
            {
                Title = request.Title.Trim(),
                Abstract = request.Abstract.Trim(),
                TechStack = request.TechStack.Trim(),
                ResearchAreaId = request.ResearchAreaId,
                StudentId = studentId,
                Status = "Pending"
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                project.Id,
                code = "PAS-2026-" + project.Id.ToString("D3"),
                message = "Proposal submitted successfully."
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMyProposal(int id, [FromBody] UpdateProposalRequest request)
        {
            var studentId = GetUserIdFromToken();
            if (studentId == null)
                return Unauthorized("Invalid token.");

            var proposal = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.StudentId == studentId);
            if (proposal == null)
                return NotFound("Proposal not found.");

            if (proposal.Status == "Matched" || proposal.Status == "Withdrawn")
                return BadRequest("Matched or withdrawn proposals cannot be edited.");

            if (string.IsNullOrWhiteSpace(request.Title) ||
                string.IsNullOrWhiteSpace(request.Abstract) ||
                string.IsNullOrWhiteSpace(request.TechStack))
            {
                return BadRequest("All fields are required.");
            }

            var areaExists = await _context.ResearchAreas.AnyAsync(r => r.Id == request.ResearchAreaId);
            if (!areaExists)
                return BadRequest("Invalid research area.");

            proposal.Title = request.Title.Trim();
            proposal.Abstract = request.Abstract.Trim();
            proposal.TechStack = request.TechStack.Trim();
            proposal.ResearchAreaId = request.ResearchAreaId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Proposal updated successfully." });
        }

        [HttpDelete("{id}/withdraw")]
        public async Task<IActionResult> WithdrawMyProposal(int id)
        {
            var studentId = GetUserIdFromToken();
            if (studentId == null)
                return Unauthorized("Invalid token.");

            var proposal = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.StudentId == studentId);
            if (proposal == null)
                return NotFound("Proposal not found.");

            if (proposal.Status == "Matched" || proposal.Status == "Withdrawn")
                return BadRequest("Matched or withdrawn proposals cannot be withdrawn.");

            proposal.Status = "Withdrawn";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Proposal withdrawn successfully." });
        }
        [HttpGet("suitable-supervisors-by-name")]
        public async Task<IActionResult> GetSuitableSupervisorsByName(string areaName)
        {
            var supervisors = await _context.SupervisorPreferences
                .Include(sp => sp.ResearchArea)
                .Where(sp => sp.ResearchArea.Name == areaName)
                .Join(_context.Users,
                    pref => pref.SupervisorId,
                    user => user.Id,
                    (pref, user) => new {
                        id = user.Id,
                        name = user.UserName 
                    })
                .ToListAsync();

            return Ok(supervisors);
        }

        [HttpPut("reassign")]
        public async Task<IActionResult> ReassignSupervisor([FromBody] ReassignRequest request)
        {
            
            var existingMatch = await _context.Matches
                .FirstOrDefaultAsync(m => m.ProjectId == request.ProjectId);

            if (existingMatch == null)
            {
                return NotFound("Match record not found for this project.");
            }

            
            existingMatch.SupervisorId = request.SupervisorId;
            existingMatch.CreatedAt = DateTime.Now; 

            await _context.SaveChangesAsync();

            return Ok(new { message = "Supervisor reassigned successfully!" });
        }

        
        public class ReassignRequest
        {
            public int ProjectId { get; set; }
            public string SupervisorId { get; set; }
        }

        public class CreateProposalRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Abstract { get; set; } = string.Empty;
            public string TechStack { get; set; } = string.Empty;
            public int ResearchAreaId { get; set; }
        }

        public class UpdateProposalRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Abstract { get; set; } = string.Empty;
            public string TechStack { get; set; } = string.Empty;
            public int ResearchAreaId { get; set; }
        }
    }
}