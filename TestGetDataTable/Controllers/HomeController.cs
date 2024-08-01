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
                string? orderDir = request.orderDir;
                int dbtotal = 0;

                var cachedData = await _cache.GetStringAsync("DataEmployee");
                var employees = new List<Employee>();

                if (!string.IsNullOrEmpty(cachedData))
                {
                    stopwatch.Start();
                    employees = JsonConvert.DeserializeObject<List<Employee>>(cachedData);

                    // Apply search filter if searchValue is provided
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        employees = employees.Where(e =>
                            e.Name.ToLower().Contains(searchValue.ToLower()) ||
                            e.Position.ToLower().Contains(searchValue.ToLower()) ||
                            e.Department.ToLower().Contains(searchValue.ToLower()) ||
                            e.Age.ToString().Contains(searchValue) ||
                            e.StartDate.ToString().Contains(searchValue) ||
                            e.Salary.ToString().Contains(searchValue)
                        ).ToList();
                    }

                    // Apply sorting
                    if (!string.IsNullOrEmpty(orderDir))
                    {
                        employees = orderColumn switch
                        {
                            0 => orderDir.ToLower() == "asc" ? employees.OrderBy(e => e.Id).ToList() : employees.OrderByDescending(e => e.Id).ToList(),
                            1 => orderDir.ToLower() == "asc" ? employees.OrderBy(e => e.Name).ToList() : employees.OrderByDescending(e => e.Name).ToList(),
                            2 => orderDir.ToLower() == "asc" ? employees.OrderBy(e => e.Position).ToList() : employees.OrderByDescending(e => e.Position).ToList(),
                            3 => orderDir.ToLower() == "asc" ? employees.OrderBy(e => e.Department).ToList() : employees.OrderByDescending(e => e.Department).ToList(),
                            4 => orderDir.ToLower() == "asc" ? employees.OrderBy(e => e.Age).ToList() : employees.OrderByDescending(e => e.Age).ToList(),
                            5 => orderDir.ToLower() == "asc" ? employees.OrderBy(e => e.StartDate).ToList() : employees.OrderByDescending(e => e.StartDate).ToList(),
                            6 => orderDir.ToLower() == "asc" ? employees.OrderBy(e => e.Salary).ToList() : employees.OrderByDescending(e => e.Salary).ToList(),
                            
                            _ => employees
                        };
                    }

                    // Pagination
                    dbtotal = employees.Count();
                    employees = employees.Skip(start).Take(length).ToList();

                    stopwatch.Stop();
                    Console.WriteLine("Elapsed Cache Time: {0} ms", stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    stopwatch.Start();
                    var query = _context.Employees.AsQueryable();

                    // Apply search filter if searchValue is provided
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        query = query.Where(e =>
                            e.Name.ToLower().Contains(searchValue.ToLower()) ||
                            e.Position.ToLower().Contains(searchValue.ToLower()) ||
                            e.Department.ToLower().Contains(searchValue.ToLower()) ||
                            e.Age.ToString().Contains(searchValue) ||
                            e.StartDate.ToString().Contains(searchValue) ||
                            e.Salary.ToString().Contains(searchValue)
                        );
                    }

                    // Apply sorting
                    if (!string.IsNullOrEmpty(orderDir))
                    {
                        query = orderColumn switch
                        {
                            0 => orderDir.ToLower() == "asc" ? query.OrderBy(e => e.Id) : query.OrderByDescending(e => e.Id),
                            1 => orderDir.ToLower() == "asc" ? query.OrderBy(e => e.Name) : query.OrderByDescending(e => e.Name),
                            2 => orderDir.ToLower() == "asc" ? query.OrderBy(e => e.Position) : query.OrderByDescending(e => e.Position),
                            3 => orderDir.ToLower() == "asc" ? query.OrderBy(e => e.Department) : query.OrderByDescending(e => e.Department),
                            4 => orderDir.ToLower() == "asc" ? query.OrderBy(e => e.Age) : query.OrderByDescending(e => e.Age),
                            5 => orderDir.ToLower() == "asc" ? query.OrderBy(e => e.StartDate) : query.OrderByDescending(e => e.StartDate),
                            6 => orderDir.ToLower() == "asc" ? query.OrderBy(e => e.Salary) : query.OrderByDescending(e => e.Salary),
                            _ => query
                        };
                    }

                    dbtotal = await query.CountAsync();
                    employees = await query.Skip(start).Take(length).ToListAsync();

                    stopwatch.Stop();
                    Console.WriteLine("Elapsed db Time: {0} ms", stopwatch.ElapsedMilliseconds);
                }

                int recordsFiltered = dbtotal;

                var response = new
                {
                    draw = draw,
                    recordsTotal = recordsFiltered,
                    recordsFiltered = recordsFiltered,
                    data = employees
                };

                return Ok(response);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

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
