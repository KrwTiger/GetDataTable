using System.ComponentModel.DataAnnotations;

namespace TestGetDataTable.Models
{
    // Example model class (replace with your actual model)
    public class Employee
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public int Age { get; set; }
        public DateTime StartDate { get; set; }
        public decimal Salary { get; set; }
    }

    // Example class for DataTable request structure (replace with actual structure if different)
    public class DataTableRequest
    {
        public int draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public string? searchValue { get; set; }
        public int orderColumn { get; set; }
        public string? orderDir { get; set; }
    }

}
