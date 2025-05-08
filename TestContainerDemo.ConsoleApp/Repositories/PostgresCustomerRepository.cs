using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestContainerDemo.ConsoleApp.Models;
using TestContainerDemo.ConsoleApp.SqlQueries;

namespace TestContainerDemo.ConsoleApp.Repositories
{
    public class PostgresCustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public PostgresCustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> CreateCustomerAsync(Customer customer)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Postgres_CreateCustomer");
                var id = await connection.QuerySingleAsync<int>(sql, customer);

                customer.Id = id;
                return id;
            }
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Postgres_GetCustomerById");
                return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
            }
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Postgres_GetAllCustomers");
                return await connection.QueryAsync<Customer>(sql);
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Postgres_UpdateCustomer");
                int rowsAffected = await connection.ExecuteAsync(sql, customer);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Postgres_DeleteCustomer");
                int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
                return rowsAffected > 0;
            }
        }

        public async Task EnsureTableCreatedAsync()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Postgres_EnsureTableCreated");
                await connection.ExecuteAsync(sql);
            }
        }
    }
}
