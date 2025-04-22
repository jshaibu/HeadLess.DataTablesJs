# HeadLess.DataTablesJs

A .NET 8 library for integrating with DataTables.js.

**Author:** John Shaibu  
**Email:** jrshaibu@gmail.com

## Structure

- **src/** – main library code  
- **tests/** – unit tests  
- **build/** – CI/CD or NuGet configs  

## HeadLess.DataTablesJs Integration Guide

This README shows you how to integrate the `HeadLess.DataTablesJs` (.NET 8) library into a .NET application.

### 1. Clone the Library

From the root of your application repository, create a `libs/` folder (if it doesn't exist) and clone the library:

```bash
mkdir -p libs
git clone https://github.com/jshaibu/HeadLess.DataTablesJs.git libs/HeadLess.DataTablesJs
```

### 2. Add the Library Project to Your Solution

If you already have a solution file (`.sln`), add the library project to it:

```bash
dotnet sln add libs/HeadLess.DataTablesJs/HeadLess.DataTablesJs.csproj
```

If you do *not* have a solution yet, create one and then add both your app and the library:

```bash
# Create a new solution
dotnet new sln -n MyApp

# Add the library and your application projects
dotnet sln MyApp.sln add libs/HeadLess.DataTablesJs/HeadLess.DataTablesJs.csproj
# Replace `src/MyApp/MyApp.csproj` with the path to your .NET app project
dotnet sln MyApp.sln add src/MyApp/MyApp.csproj
```

### 3. Reference the Library from Your .NET App

Inside your .NET application directory:

```bash
# Adjust path as necessary to point to the library project
dotnet add reference ../../libs/HeadLess.DataTablesJs/HeadLess.DataTablesJs.csproj
```

This will add a `<ProjectReference>` entry to your app's `.csproj` file under an `<ItemGroup>`.

### 4. Build and Run Your Solution

```bash
# From your solution root (where the .sln file lives)
dotnet build

dotnet run --project src/MyApp/MyApp.csproj
```

Your .NET application will now reference and include the .NET 8 library automatically.

### 5. (Optional) Pack & Consume as a NuGet Package

If you'd rather consume the library as a NuGet package:

1. **Pack the library**
   ```bash
   cd libs/HeadLess.DataTablesJs
   dotnet pack -c Release
   ```
2. **Add the package to your app**
   ```bash
    # In your app directory
    dotnet add package HeadLess.DataTablesJs --source libs/HeadLess.DataTablesJs/bin/Release
    ```


## USAGE ON BACKEND 


// README Usage Example: DataTablesJs Integration in Controller

```C#
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
```

## USAGE ON FRONTEND

Frontend Setup (DataTables with server-side or local data)

```javascript
const columns = [
  { title: '#', data: 'counter', orderable: false, searchable: false },
  { title: 'Full Name', data: 'fullName', orderable: true, searchable: true },
  { title: 'Payment Date', data: 'paymentDate', orderable: true, searchable: true },
  { title: 'Amount Paid', data: 'amountPaid', orderable: true, searchable: true },
  { title: 'Method Name', data: 'methodName', orderable: true, searchable: true }
];

const table = $('#datatable').DataTable({
  processing: true,
  serverSide: true,
  ajax: {
    url: '/api/invoices/datatables', // your .NET API endpoint
    type: 'POST',
    contentType: 'application/json',
    data: function (d) {
      return JSON.stringify(d); // send JSON instead of form-urlencoded
    }
  },
  columns: columns,
  pagingType: 'simple_numbers',
  language: {
    search: '_INPUT_',
    searchPlaceholder: 'Search...',
    paginate: {
      previous: 'Previous',
      next: 'Next'
    },
    aria: {
      sortAscending: 'sorting_asc',
      sortDescending: 'sorting_desc'
    }
  },
  columnDefs: [
    {
      targets: '_all',
      createdCell: function (td) {
        $(td).addClass('sorting');
      }
    }
  ]
});

```

### Happy coding!