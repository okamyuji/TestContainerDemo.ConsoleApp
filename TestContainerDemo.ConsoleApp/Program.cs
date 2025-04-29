using System;
using System.Threading.Tasks;
using TestContainerDemo.ConsoleApp.Models;
using TestContainerDemo.ConsoleApp.Repositories;

namespace TestContainerDemo.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("TestContainer Demo Application Starting...");

            // コマンドライン引数からデータベースタイプを取得
            string dbType = args.Length > 0 ? args[0].ToLower() : "postgres";

            // 環境変数から接続文字列を取得
            string connectionString = Environment.GetEnvironmentVariable(
                dbType == "oracle" ? "ORACLE_CONNECTION_STRING" : "POSTGRES_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine($"Error: Connection string for {dbType} is not set. Please set the environment variable.");
                Console.ReadKey();
                return;
            }

            try
            {
                ICustomerRepository repository;

                // リポジトリの登録
                if (dbType == "oracle")
                {
                    repository = new OracleCustomerRepository(connectionString);
                    await ((OracleCustomerRepository)repository).EnsureTableCreatedAsync();
                }
                else
                {
                    repository = new PostgresCustomerRepository(connectionString);
                    await ((PostgresCustomerRepository)repository).EnsureTableCreatedAsync();
                }

                Console.WriteLine($"Using {dbType} database.");

                // CRUD操作のデモ
                await DemonstrateCrudOperationsAsync(repository);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task DemonstrateCrudOperationsAsync(ICustomerRepository repository)
        {
            Console.WriteLine("\n===== CRUD Operations Demo =====\n");

            // 1. Create
            Console.WriteLine("Creating a new customer...");
            Customer newCustomer = new Customer
            {
                Name = "山田太郎",
                Email = "taro.yamada@example.com",
                CreatedAt = DateTime.Now
            };

            int newId = await repository.CreateCustomerAsync(newCustomer);
            Console.WriteLine($"Created customer with ID: {newId}");

            // 2. Read
            Console.WriteLine("\nReading customer by ID...");
            Customer retrievedCustomer = await repository.GetCustomerByIdAsync(newId);
            Console.WriteLine($"Retrieved: {retrievedCustomer}");

            // 3. Update
            Console.WriteLine("\nUpdating customer...");
            if (retrievedCustomer != null)
            {
                retrievedCustomer.Name = "山田次郎";
                retrievedCustomer.Email = "jiro.yamada@example.com";
                bool updateResult = await repository.UpdateCustomerAsync(retrievedCustomer);
                Console.WriteLine($"Update successful: {updateResult}");

                // 更新後のデータを取得
                Customer updatedCustomer = await repository.GetCustomerByIdAsync(newId);
                Console.WriteLine($"After update: {updatedCustomer}");
            }

            // 別の顧客を追加
            Customer secondCustomer = new Customer
            {
                Name = "田中花子",
                Email = "hanako.tanaka@example.com",
                CreatedAt = DateTime.Now
            };
            await repository.CreateCustomerAsync(secondCustomer);

            // 4. Read All
            Console.WriteLine("\nReading all customers...");
            System.Collections.Generic.IEnumerable<Customer> allCustomers = await repository.GetAllCustomersAsync();
            Console.WriteLine("All customers:");
            foreach (Customer customer in allCustomers)
            {
                Console.WriteLine(customer);
            }

            // 5. Delete
            Console.WriteLine("\nDeleting customer...");
            bool deleteResult = await repository.DeleteCustomerAsync(newId);
            Console.WriteLine($"Delete successful: {deleteResult}");

            // 削除後のデータを取得
            System.Collections.Generic.IEnumerable<Customer> allRemainingCustomers = await repository.GetAllCustomersAsync();
            Console.WriteLine("\nRemaining customers after delete:");
            foreach (Customer customer in allRemainingCustomers)
            {
                Console.WriteLine(customer);
            }

            Console.WriteLine("\n===== Demo Completed =====");
        }
    }
}