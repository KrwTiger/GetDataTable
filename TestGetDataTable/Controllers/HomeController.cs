using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Diagnostics;
using TestGetDataTable.Context;
using TestGetDataTable.Models;
namespace TestGetDataTable.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDistributedCache _cache;
        private readonly ApplicationDbContext _context; // เชื่อมต่อกับ DbContext ของคุณที่มีชื่อว่า ApplicationDbContext
        private readonly ILogger<HomeController> _logger;

        public HomeController(IDistributedCache cache, ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _cache = cache;
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            //try
            //{
            //    SaveEmployee();
            //}
            //catch { throw new NotImplementedException(); }
            _cache.RemoveAsync("DataEmployee");
            return View();
        }
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public void SaveEmployee()
        {
            var random = new Random();
            var positions = new[] { "Developer", "Manager", "Analyst", "Tester", "Support" };
            var departments = new[] { "IT", "HR", "Finance", "Sales", "Support", "Engineer" };

            for (int i = 0; i < 500000; i++)
            {
                Employee obj = new Employee()
                {
                    Name = "Employee" + i,
                    Position = positions[random.Next(positions.Length)],
                    Department = departments[random.Next(departments.Length)],
                    Age = random.Next(20, 60),
                    StartDate = DateTime.Now.AddDays(-random.Next(0, 365)),
                    Salary = Convert.ToUInt32(random.Next(10, 150).ToString() + "000")
                };
                _context.Employees.Add(obj);
            }

            _context.SaveChangesAsync();
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataTableRequest request)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();

                // Parameters from DataTable
                int draw = request.draw;
                int start = request.start;
                int length = request.length;
                string? searchValue = request.searchValue?.ToLower();
                int orderColumn = request.orderColumn;
                string? orderDir = request.orderDir?.ToLower();

                var employees = await GetEmployeesAsync(searchValue, orderColumn, orderDir, start, length);
                int recordsFiltered = employees.totalCount;

                var response = new
                {
                    draw = draw,
                    recordsTotal = recordsFiltered,
                    recordsFiltered = recordsFiltered,
                    data = employees.list
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        async Task<(List<Employee> list, int totalCount)> GetEmployeesAsync(string? searchValue, int orderColumn, string? orderDir, int start, int length)
        {
            var cachedData = await _cache.GetStringAsync("DataEmployee");
            if (!string.IsNullOrEmpty(cachedData))
            {
                var employees = JsonConvert.DeserializeObject<List<Employee>>(cachedData);
                return await ProcessEmployeesAsync(employees.AsQueryable(), searchValue, orderColumn, orderDir, start, length, true);
            }
            else
            {
                var query = _context.Employees.AsQueryable();
                return await ProcessEmployeesAsync(query, searchValue, orderColumn, orderDir, start, length, false);
            }
        }

        async Task<(List<Employee> list, int totalCount)> ProcessEmployeesAsync(IQueryable<Employee> query, string? searchValue, int orderColumn, string? orderDir, int start, int length, bool fromCache)
{
    Stopwatch stopwatch = new Stopwatch();
    stopwatch.Start();

    if (!string.IsNullOrEmpty(searchValue))
    {
        query = query.Where(e =>
            e.Name.ToLower().Contains(searchValue) ||
            e.Position.ToLower().Contains(searchValue) ||
            e.Department.ToLower().Contains(searchValue) ||
            e.Age.ToString().Contains(searchValue) ||
            e.StartDate.ToString().Contains(searchValue) ||
            e.Salary.ToString().Contains(searchValue)
        );
    }

    if (!string.IsNullOrEmpty(orderDir))
    {
        query = orderColumn switch
        {
            0 => orderDir == "asc" ? query.OrderBy(e => e.Id) : query.OrderByDescending(e => e.Id),
            1 => orderDir == "asc" ? query.OrderBy(e => e.Name) : query.OrderByDescending(e => e.Name),
            2 => orderDir == "asc" ? query.OrderBy(e => e.Position) : query.OrderByDescending(e => e.Position),
            3 => orderDir == "asc" ? query.OrderBy(e => e.Department) : query.OrderByDescending(e => e.Department),
            4 => orderDir == "asc" ? query.OrderBy(e => e.Age) : query.OrderByDescending(e => e.Age),
            5 => orderDir == "asc" ? query.OrderBy(e => e.StartDate) : query.OrderByDescending(e => e.StartDate),
            6 => orderDir == "asc" ? query.OrderBy(e => e.Salary) : query.OrderByDescending(e => e.Salary),
            _ => query
        };
    }

    int totalCount = fromCache ? query.Count() : await query.CountAsync();
    var employees = fromCache ? query.Skip(start).Take(length).ToList() : await query.Skip(start).Take(length).ToListAsync();

    stopwatch.Stop();
    Console.WriteLine("Elapsed {0} Time: {1} ms", fromCache ? "Cache" : "db", stopwatch.ElapsedMilliseconds);

    return (employees, totalCount);
}

        public async Task<IActionResult> GetCache(string key)
        {
            var cachedData = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return Content("use cache in memory ");
            }

            var data = await _context.Employees.ToListAsync();
            var serializedData = JsonConvert.SerializeObject(data);

            await _cache.SetStringAsync(key, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });


            return Content("cache is updated ");
        }

    }
}
