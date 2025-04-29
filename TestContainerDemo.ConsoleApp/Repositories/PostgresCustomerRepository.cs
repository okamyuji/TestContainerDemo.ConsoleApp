using Npgsql;
using System;
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
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    INSERT INTO customers (name, email, created_at) 
                    VALUES (@Name, @Email, @CreatedAt) 
                    RETURNING id";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("Name", customer.Name);
                    command.Parameters.AddWithValue("Email", customer.Email);
                    command.Parameters.AddWithValue("CreatedAt", customer.CreatedAt);

                    // PostgreSQLは直接RETURNING句を使用して生成されたIDを取得できる
                    object id = await command.ExecuteScalarAsync();
                    customer.Id = Convert.ToInt32(id);
                    return customer.Id;
                }
            }
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT id, name, email, created_at FROM customers WHERE id = @Id";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("Id", id);

                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Customer
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3)
                            };
                        }
                    }
                }

                return null;
            }
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            List<Customer> customers = new List<Customer>();

            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT id, name, email, created_at FROM customers";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        customers.Add(new Customer
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Email = reader.GetString(2),
                            CreatedAt = reader.GetDateTime(3)
                        });
                    }
                }
            }

            return customers;
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    UPDATE customers 
                    SET name = @Name, email = @Email 
                    WHERE id = @Id";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("Name", customer.Name);
                    command.Parameters.AddWithValue("Email", customer.Email);
                    command.Parameters.AddWithValue("Id", customer.Id);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "DELETE FROM customers WHERE id = @Id";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("Id", id);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        // テーブル作成用メソッド（初期化用）
        public async Task EnsureTableCreatedAsync()
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    CREATE TABLE IF NOT EXISTS customers (
                        id SERIAL PRIMARY KEY,
                        name VARCHAR(100) NOT NULL,
                        email VARCHAR(100) UNIQUE NOT NULL,
                        created_at TIMESTAMP NOT NULL
                    )";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}