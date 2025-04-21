# HeadLess.DataTablesJs

A .NET 8 library for integrating with DataTables.js.

**Author:** John Shaibu  
**Email:** jrshaibu@gmail.com

## Structure

- **src/** – main library code  
- **tests/** – unit tests  
- **build/** – CI/CD or NuGet configs  



----------------------------------------------------------------- USAGE -------------------------------------------------------


// README Usage Example: DataTablesJs Integration in Controller

// --- Program.cs Configuration ---
// Ensure the following is added in your Program.cs or Startup.cs
using HeadLess.DataTables.Extensions;

builder.Services.AddScoped<IDataTablesService, DataTablesService>();

// If using EF Core:
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// --- Controller Example ---
using HeadLess.DataTablesJs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IDataTablesService _dataTablesService;
    private readonly AppDbContext _context;

    public InvoicesController(IDataTablesService dataTablesService, AppDbContext context)
    {
        _dataTablesService = dataTablesService;
        _context = context;
    }

    // Example A: Using IQueryable<T>
    [HttpPost("query")]
    public async Task<IActionResult> GetInvoicesViaQuery([FromBody] DataTablesRequest request)
    {
        var query = _context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Company)
            .Select(i => new InvoiceQuery
            {
                InvoiceNumber = i.InvoiceNumber,
                FirstName = i.Student.FirstName,
                LastName = i.Student.LastName,
                CompanyName = i.Company.CompanyName,
                AmountDue = i.AmountDue,
                DueDate = i.DueDate,
                PaidDate = i.PaidDate,
            })
            .OrderBy(i => i.FirstName);

        var result = await _dataTablesService.GetDataAsync<InvoiceQuery>(request, query);
        return Ok(result);
    }

    // Example B: Using List<T> if the query is complex
    [HttpPost("list")]
    public async Task<IActionResult> GetInvoicesViaList([FromBody] DataTablesRequest request)
    {
        var query = _context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Company)
            .Select(i => new InvoiceQuery
            {
                InvoiceNumber = i.InvoiceNumber,
                FirstName = i.Student.FirstName,
                LastName = i.Student.LastName,
                CompanyName = i.Company.CompanyName,
                AmountDue = i.AmountDue,
                DueDate = i.DueDate,
                PaidDate = i.PaidDate,
            })
            .OrderBy(i => i.FirstName);

        var result = await _dataTablesService.GetDataAsync<InvoiceQuery>(request, await query.ToListAsync());
        return Ok(result);
    }
}

// --- Example DTO ---
public class InvoiceQuery
{
    public string InvoiceNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public decimal AmountDue { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
}






