using System.Collections.Generic;
using System.Threading.Tasks;
using TestContainerDemo.ConsoleApp.Models;

namespace TestContainerDemo.ConsoleApp.Repositories
{
    public interface ICustomerRepository
    {
        Task<int> CreateCustomerAsync(Customer customer);
        Task<Customer> GetCustomerByIdAsync(int id);
        Task<IEnumerable<Customer>> GetAllCustomersAsync();
        Task<bool> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(int id);
    }
}