using Microsoft.EntityFrameworkCore;
using TestGetDataTable.Models;
//using TestGetDataTable.Models.DB;

namespace TestGetDataTable.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> contextOptions) : base(contextOptions) { }

        public DbSet<Employee> Employees { get; set; }

        //public DbSet<EDPViewModel> EDPs { get; set; }

    }
}