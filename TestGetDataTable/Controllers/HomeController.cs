using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Diagnostics;
using TestGetDataTable.Context;
using TestGetDataTable.Models;
using StackExchange.Redis;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.Collections;

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

                IQueryable<Employee> query = _context.Employees;
                // เช็คข้อมูลใน cache
                //string cacheKey = $"DataEmployee_{start}_{length}_{searchValue}_{orderColumn}_{orderDir}";
                var cachedData = await _cache.GetStringAsync("DataEmployee");
                var employees = new List<Employee>();
                if (!string.IsNullOrEmpty(cachedData))
                {
                    stopwatch.Start();
                    //if (string.IsNullOrEmpty(searchValue))
                    //{
                        employees = JsonConvert.DeserializeObject<List<Employee>>(cachedData);
                    //}
                    //else if (string.IsNullOrEmpty(searchValue) && orderColumn == 0 && orderDir.ToLower() == "asc")
                    //{
                    //    employees = employees.Take(10).ToList();
                    //}
                    stopwatch.Stop();
                    Console.WriteLine("Elapsed Cache Time: {0} ms", stopwatch.ElapsedMilliseconds);

                }
                else
                {

                    stopwatch.Start();
                    //_logger.LogInformation(recordsTotal.ToString());
                    dbtotal = await query.CountAsync();
                    employees = await query.Skip(start).Take(length).ToListAsync();
                    stopwatch.Stop();
                    Console.WriteLine("Elapsed db Time: {0} ms", stopwatch.ElapsedMilliseconds);

                    //var serializedEmployees = JsonConvert.SerializeObject(employees);
                    //await _cache.SetStringAsync(cacheKey, serializedEmployees, new DistributedCacheEntryOptions
                    //{
                    //    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // ปรับเวลาหมดอายุของ cache ตามต้องการ
                    //});
                }

                stopwatch.Start();
                if (!string.IsNullOrEmpty(searchValue))
                {
                    employees = employees.Where(e =>
                        e.Name.ToLower().Contains(searchValue) ||
                        e.Position.ToLower().Contains(searchValue) ||
                        e.Department.ToLower().Contains(searchValue) ||
                        e.Age.ToString().Contains(searchValue) ||
                        e.StartDate.ToString().Contains(searchValue) ||
                        e.Salary.ToString().Contains(searchValue)
                    ).ToList();
                }
                stopwatch.Stop();
                Console.WriteLine("Elapsed search Time: {0} ms", stopwatch.ElapsedMilliseconds);

                //// Apply sorting
                stopwatch.Start();
                if (!string.IsNullOrEmpty(orderDir))
                {
                    switch (orderColumn)
                    {
                        case 0: // Index 0 is column Name
                            employees = (orderDir.ToLower() == "asc") ? employees.OrderBy(e => e.Name).ToList() : employees.OrderByDescending(e => e.Name).ToList();
                            break;
                        case 1: // Index 1 is column Position
                            employees = (orderDir.ToLower() == "asc") ? employees.OrderBy(e => e.Position).ToList() : employees.OrderByDescending(e => e.Position).ToList();
                            break;
                        case 2: // Index 2 is column Department
                            employees = (orderDir.ToLower() == "asc") ? employees.OrderBy(e => e.Department).ToList() : employees.OrderByDescending(e => e.Department).ToList();
                            break;
                        case 3: // Index 3 is column Age
                            employees = (orderDir.ToLower() == "asc") ? employees.OrderBy(e => e.Age).ToList() : employees.OrderByDescending(e => e.Age).ToList();
                            break;
                        case 4: // Index 4 is column StartDate
                            employees = (orderDir.ToLower() == "asc") ? employees.OrderBy(e => e.StartDate).ToList() : employees.OrderByDescending(e => e.StartDate).ToList();
                            break;
                        case 5: // Index 5 is column Salary
                            employees = (orderDir.ToLower() == "asc") ? employees.OrderBy(e => e.Salary).ToList() : employees.OrderByDescending(e => e.Salary).ToList();
                            break;
                        default:
                            break;
                    }
                }
                stopwatch.Stop();
                Console.WriteLine("Elapsed sorting Time: {0} ms", stopwatch.ElapsedMilliseconds);

                stopwatch.Start();
                int recordsTotal = dbtotal != 0 ? dbtotal : employees.Count();
                var data = employees.Skip(start).Take(length).ToList();
                stopwatch.Stop();
                Console.WriteLine("Elapsed reponse Time: {0} ms", stopwatch.ElapsedMilliseconds);

                var response = new
                {
                    draw = draw,
                    recordsTotal = recordsTotal,
                    recordsFiltered = recordsTotal,
                    data = data
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
                return Content(cachedData);
            }

            // ดึงข้อมูลจากฐานข้อมูล
            var data = await _context.Employees.ToListAsync();
            var serializedData = JsonConvert.SerializeObject(data);

            // เก็บข้อมูลลงใน cache
            await _cache.SetStringAsync(key, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            //var serializedEmployees = JsonConvert.SerializeObject(employees);
            //await _cache.SetStringAsync(cacheKey, serializedEmployees, new DistributedCacheEntryOptions
            //{
            //    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // ปรับเวลาหมดอายุของ cache ตามต้องการ
            //});

            return Content(serializedData);
        }

    }
}
