
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ssw2026TermProject.Models;
using ssw2026TermProject.Data;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: CUSTOMERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Customers.ToListAsync());
    }

    // GET: CUSTOMERS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.ID == id);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // GET: CUSTOMERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CUSTOMERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,FirstName,MiddleName,LastName,DOB,Municipality,Province,PostalCode,Phone,Email,Address")] Customer customer)
    {
        if (customer.DOB == default)
        {
            ModelState.AddModelError(
                nameof(customer.DOB),
                "Date of Birth is required.");
        }
        else if (customer.DOB.Date > DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(customer.DOB),
                "Date of Birth cannot be in the future.");
        }
        else if (customer.DOB.Date > DateTime.Today.AddYears(-19))
        {
            ModelState.AddModelError(
                nameof(customer.DOB),
                "Agent must be at least 19 years old.");
        }
        if (ModelState.IsValid)
        {
            
            _context.Add(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    // GET: CUSTOMERS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        return View(customer);
    }

    // POST: CUSTOMERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,FirstName,MiddleName,LastName,DOB,Municipality,Phone,Email,Address")] Customer customer)
    {
        if (id != customer.ID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(customer.ID))
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
        return View(customer);
    }

    // GET: CUSTOMERS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.ID == id);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // POST: CUSTOMERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        // E2 新增：客户已经有房源或预约时不能删除（否则会破坏这些记录）
        bool inUse = await _context.Listings.AnyAsync(l => l.CustomerId == id)
                  || await _context.Showings.AnyAsync(s => s.CustomerId == id);
        if (inUse)
        {
            TempData["Error"] = "This customer has listings or showings and cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CustomerExists(int? id)
    {
        return _context.Customers.Any(e => e.ID == id);
    }
}
