using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TestGetDataTable.Context;
using TestGetDataTable.Models;

namespace TestGetDataTable.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context; // เชื่อมต่อกับ DbContext ของคุณที่มีชื่อว่า ApplicationDbContext
        private readonly ILogger<HomeController> _logger;
        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
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
            //   SaveEmployee();
            //}
            //catch { throw new NotImplementedException(); }
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

            for (int i = 0; i < 100000; i++)
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
        public async Task<IActionResult> GetData([FromBody]  DataTableRequest request)
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
                
                //// Query data from database
                IQueryable<Employee> query = _context.Employees;
               
                //// Apply search filter
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

                // Apply ordering
                if (!string.IsNullOrEmpty(orderDir))
                {
                    switch (orderColumn)
                    {
                        case 0: // Index 0 คือคอลัมน์ Name
                            query = (orderDir.ToLower() == "asc") ? query.OrderBy(e => e.Name) : query.OrderByDescending(e => e.Name);
                            break;
                        case 1: // Index 1 คือคอลัมน์ Position
                            query = (orderDir.ToLower() == "asc") ? query.OrderBy(e => e.Position) : query.OrderByDescending(e => e.Position);
                            break;
                        case 2: // Index 2 คือคอลัมน์ Department
                            query = (orderDir.ToLower() == "asc") ? query.OrderBy(e => e.Department) : query.OrderByDescending(e => e.Department);
                            break;
                        case 3: // Index 3 คือคอลัมน์ Age
                            query = (orderDir.ToLower() == "asc") ? query.OrderBy(e => e.Age) : query.OrderByDescending(e => e.Age);
                            break;
                        case 4: // Index 4 คือคอลัมน์ StartDate
                            query = (orderDir.ToLower() == "asc") ? query.OrderBy(e => e.StartDate) : query.OrderByDescending(e => e.StartDate);
                            break;
                        case 5: // Index 5 คือคอลัมน์ Salary
                            query = (orderDir.ToLower() == "asc") ? query.OrderBy(e => e.Salary) : query.OrderByDescending(e => e.Salary);
                            break;
                        default:
                            break;
                    }
                }

                //// Count total records before pagination
                int recordsTotal = await query.CountAsync();
                stopwatch.Start();
                //// Apply paginations
                _logger.LogInformation(recordsTotal.ToString());
                //List<Employee> data = await query.Skip(start).Take(length).ToListAsync();
                var data = await query.Skip(start).Take(length).ToListAsync();
                stopwatch.Stop();
                Console.WriteLine("Elapsed Time: {0} ms", stopwatch.ElapsedMilliseconds);


                //// Prepare response for DataTable
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
    }
}
