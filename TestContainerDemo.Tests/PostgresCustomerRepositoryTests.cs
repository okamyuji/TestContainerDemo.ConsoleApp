using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using TestContainerDemo.ConsoleApp.Models;
using TestContainerDemo.ConsoleApp.Repositories;
using TestContainerDemo.Tests.Helpers;
using TestContainerDemo.Tests.SqlQueries;

namespace TestContainerDemo.Tests
{
    [TestClass]
    public class PostgresCustomerRepositoryTests
    {
        private static DockerContainerHelper _postgresContainer;
        private static string _connectionString;
        private static PostgresCustomerRepository _repository;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // SQLクエリマネージャの初期化を確認（エラー時には早期に検出）
            try
            {
                // テスト用のSqlQueryManagerにアクセス
                var dummyQuery = SqlQueryManager.GetQuery("Postgres_GetAllCustomers");
                Console.WriteLine("テスト用SQLクエリの読み込みが完了しました。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"テスト用SQLクエリの読み込みに失敗しました: {ex.Message}");
                throw;
            }

            // コンテナ名を一意にするためにGUIDを使用
            string containerName = $"postgres-test-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            // ポートを5432以外のランダムなポートにマッピング（既存のポート競合を避ける）
            string hostPort = "15432"; // 別のポートを使用

            Console.WriteLine("PostgreSQLテスト用コンテナを準備しています...");

            // PostgreSQLコンテナの設定 - リストにあるイメージ名とタグを使用
            _postgresContainer = new DockerContainerHelper("postgres:latest")
                .WithName(containerName)
                .WithEnvironment("POSTGRES_USER", "postgres")
                .WithEnvironment("POSTGRES_PASSWORD", "postgres")
                .WithEnvironment("POSTGRES_DB", "testdb")
                .WithPortMapping(hostPort, "5432/tcp");

            await _postgresContainer.StartAsync();

            // 動的に割り当てられたポートを使用
            _connectionString = $"Host=localhost;Port={hostPort};Database=testdb;Username=postgres;Password=postgres";

            Console.WriteLine("PostgreSQL接続文字列: " + _connectionString);

            // コンテナが完全に起動するまで少し待機
            Console.WriteLine("PostgreSQLサービスの準備ができるまで待機しています...");
            await Task.Delay(TimeSpan.FromSeconds(10));

            // リポジトリの初期化とテーブルの作成
            _repository = new PostgresCustomerRepository(_connectionString);
            await _repository.EnsureTableCreatedAsync();
            Console.WriteLine("PostgreSQLテストの準備が完了しました。");
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static async Task ClassCleanup()
        {
            // テスト終了後にコンテナを停止
            if (_postgresContainer != null)
            {
                await _postgresContainer.StopAsync();
            }
        }

        [TestMethod]
        public async Task CreateCustomer_ShouldReturnId()
        {
            // Arrange
            Customer customer = new Customer
            {
                Name = "Test User",
                Email = $"test_{Guid.NewGuid()}@example.com",
                CreatedAt = DateTime.Now
            };

            // Act
            int id = await _repository.CreateCustomerAsync(customer);

            // Assert
            Assert.IsTrue(id > 0);
        }

        [TestMethod]
        public async Task GetCustomerById_ShouldReturnCustomer()
        {
            // Arrange
            Customer customer = new Customer
            {
                Name = "Test User",
                Email = $"get_{Guid.NewGuid()}@example.com",
                CreatedAt = DateTime.Now
            };
            int id = await _repository.CreateCustomerAsync(customer);

            // Act
            Customer result = await _repository.GetCustomerByIdAsync(id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(id, result.Id);
            Assert.AreEqual("Test User", result.Name);
            Assert.AreEqual(customer.Email, result.Email);
        }

        [TestMethod]
        public async Task UpdateCustomer_ShouldReturnTrue()
        {
            // Arrange
            Customer customer = new Customer
            {
                Name = "Original Name",
                Email = $"update_{Guid.NewGuid()}@example.com",
                CreatedAt = DateTime.Now
            };
            int id = await _repository.CreateCustomerAsync(customer);

            Customer retrievedCustomer = await _repository.GetCustomerByIdAsync(id);
            retrievedCustomer.Name = "Updated Name";
            retrievedCustomer.Email = $"updated_{Guid.NewGuid()}@example.com";

            // Act
            bool result = await _repository.UpdateCustomerAsync(retrievedCustomer);

            // Assert
            Assert.IsTrue(result);

            // 検証
            Customer updatedCustomer = await _repository.GetCustomerByIdAsync(id);
            Assert.AreEqual("Updated Name", updatedCustomer.Name);
            Assert.AreEqual(retrievedCustomer.Email, updatedCustomer.Email);
        }

        [TestMethod]
        public async Task DeleteCustomer_ShouldReturnTrue()
        {
            // Arrange
            Customer customer = new Customer
            {
                Name = "To Delete",
                Email = $"delete_{Guid.NewGuid()}@example.com",
                CreatedAt = DateTime.Now
            };
            int id = await _repository.CreateCustomerAsync(customer);

            // Act
            bool result = await _repository.DeleteCustomerAsync(id);

            // Assert
            Assert.IsTrue(result);

            // 削除の検証
            Customer deletedCustomer = await _repository.GetCustomerByIdAsync(id);
            Assert.IsNull(deletedCustomer);
        }
    }
}