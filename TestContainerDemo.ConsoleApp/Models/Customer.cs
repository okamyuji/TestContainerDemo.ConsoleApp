using System;

namespace TestContainerDemo.ConsoleApp.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, Name: {Name}, Email: {Email}, CreatedAt: {CreatedAt}";
        }
    }
}