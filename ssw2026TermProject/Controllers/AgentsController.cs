
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Data;
using ssw2026TermProject.Models;
using System.Security.Claims;

[Authorize]
public class AgentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AgentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: AGENTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Agents.ToListAsync());
    }

    // GET: AGENTS/Details/5
    public async Task<IActionResult> Details(int? agentid)
    {
        if (agentid == null)
        {
            return NotFound();
        }

        var agent = await _context.Agents
            .FirstOrDefaultAsync(m => m.AgentId == agentid);
        if (agent == null)
        {
            return NotFound();
        }

        return View(agent);
    }

    // GET: AGENTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: AGENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AgentId,FirstName,MiddleName,LastName,Username,SIN,DateOfBirth,StreetAddress,City,Province,PostalCode,MobilePhone,OfficePhone,HomePhone,Email,HasVerifiedGovernmentId,DateOfEmployment,CreatedByStaffId")] Agent agent)
    {
        if (!string.IsNullOrWhiteSpace(agent.Username))
        {
            bool usernameExists = await _context.Agents
                .AnyAsync(a => a.Username == agent.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    nameof(agent.Username),
                    "This username already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(agent.SIN))
        {
            bool sinExists = await _context.Agents
                .AnyAsync(a => a.SIN == agent.SIN);

            if (sinExists)
            {
                ModelState.AddModelError(
                    nameof(agent.SIN),
                    "This SIN already exists.");
            }
        }

        if (agent.DateOfBirth == default)
        {
            ModelState.AddModelError(
                nameof(agent.DateOfBirth),
                "Date of Birth is required.");
        }
        else if (agent.DateOfBirth.Date > DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(agent.DateOfBirth),
                "Date of Birth cannot be in the future.");
        }
        else if (agent.DateOfBirth.Date > DateTime.Today.AddYears(-19))
        {
            ModelState.AddModelError(
                nameof(agent.DateOfBirth),
                "Agent must be at least 19 years old.");
        }

        if (!agent.HasVerifiedGovernmentId)
        {
            ModelState.AddModelError(
                nameof(agent.HasVerifiedGovernmentId),
                "You must confirm that you have viewed the government-issued ID.");
        }


        if (ModelState.IsValid)
        {
            agent.CreatedByStaffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            agent.DateOfEmployment = DateTime.Now;
            _context.Add(agent);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(agent);
    }

    // GET: AGENTS/Edit/5
    public async Task<IActionResult> Edit(int? agentid)
    {
        if (agentid == null)
        {
            return NotFound();
        }

        var agent = await _context.Agents.FindAsync(agentid);
        if (agent == null)
        {
            return NotFound();
        }
        return View(agent);
    }

    // POST: AGENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? agentid, [Bind("AgentId,FirstName,MiddleName,LastName,Username,SIN,DateOfBirth,StreetAddress,City,Province,PostalCode,MobilePhone,OfficePhone,HomePhone,Email,HasVerifiedGovernmentId,DateOfEmployment,CreatedByStaffId")] Agent agent)
    {
        if (agentid != agent.AgentId)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(agent.Username))
        {
            bool usernameExists = await _context.Agents
                .AnyAsync(a => a.Username == agent.Username
                            && a.AgentId != agent.AgentId);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    nameof(agent.Username),
                    "This username already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(agent.SIN))
        {
            bool sinExists = await _context.Agents
                .AnyAsync(a => a.SIN == agent.SIN
                            && a.AgentId != agent.AgentId);

            if (sinExists)
            {
                ModelState.AddModelError(
                    nameof(agent.SIN),
                    "This SIN already exists.");
            }
        }

        if (agent.DateOfBirth == default)
        {
            ModelState.AddModelError(
                nameof(agent.DateOfBirth),
                "Date of Birth is required.");
        }
        else if (agent.DateOfBirth.Date > DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(agent.DateOfBirth),
                "Date of Birth cannot be in the future.");
        }
        else if (agent.DateOfBirth.Date > DateTime.Today.AddYears(-16))
        {
            ModelState.AddModelError(
                nameof(agent.DateOfBirth),
                "Agent must be at least 16 years old.");
        }

        if (!agent.HasVerifiedGovernmentId)
        {
            ModelState.AddModelError(
                nameof(agent.HasVerifiedGovernmentId),
                "You must confirm that you have viewed the government-issued ID.");
        }
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(agent);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AgentExists(agent.AgentId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(agent);
    }

    // GET: AGENTS/Delete/5
    public async Task<IActionResult> Delete(int? agentid)
    {
        if (agentid == null)
        {
            return NotFound();
        }

        var agent = await _context.Agents
            .FirstOrDefaultAsync(m => m.AgentId == agentid);
        if (agent == null)
        {
            return NotFound();
        }

        return View(agent);
    }

    // POST: AGENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? agentid)
    {
        var agent = await _context.Agents.FindAsync(agentid);
        if (agent != null)
        {
            _context.Agents.Remove(agent);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AgentExists(int? agentid)
    {
        return _context.Agents.Any(e => e.AgentId == agentid);
    }


}
