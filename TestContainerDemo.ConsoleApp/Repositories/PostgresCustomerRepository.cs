using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestContainerDemo.ConsoleApp.Models;

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
                // PostgreSQLは直接RETURNING句でIDを取得できる
                var id = await connection.QuerySingleAsync<int>(@"
                    INSERT INTO customers (name, email, created_at) 
                    VALUES (@Name, @Email, @CreatedAt) 
                    RETURNING id",
                    customer);

                customer.Id = id;
                return id;
            }
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                return await connection.QuerySingleOrDefaultAsync<Customer>(
                    "SELECT id, name, email, created_at FROM customers WHERE id = @Id",
                    new { Id = id });
            }
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                return await connection.QueryAsync<Customer>(
                    "SELECT id, name, email, created_at FROM customers");
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                int rowsAffected = await connection.ExecuteAsync(@"
                    UPDATE customers 
                    SET name = @Name, email = @Email 
                    WHERE id = @Id",
                    customer);

                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                int rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM customers WHERE id = @Id",
                    new { Id = id });

                return rowsAffected > 0;
            }
        }

        // テーブル作成用メソッド（初期化用）
        public async Task EnsureTableCreatedAsync()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS customers (
                        id SERIAL PRIMARY KEY,
                        name VARCHAR(100) NOT NULL,
                        email VARCHAR(100) UNIQUE NOT NULL,
                        created_at TIMESTAMP NOT NULL
                    )");
            }
        }
    }
}