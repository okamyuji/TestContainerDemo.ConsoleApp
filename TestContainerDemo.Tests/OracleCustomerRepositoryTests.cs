using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using TestContainerDemo.ConsoleApp.Models;
using TestContainerDemo.ConsoleApp.Repositories;
using TestContainerDemo.Tests.Helpers;

namespace TestContainerDemo.Tests
{
    [TestClass]
    public class OracleCustomerRepositoryTests
    {
        private static DockerContainerHelper _oracleContainer;
        private static string _connectionString;
        private static OracleCustomerRepository _repository;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            // コンテナ名を一意にするためにGUIDを使用
            string containerName = $"oracle-test-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            // ポートを1521以外のランダムなポートにマッピング（既存のポート競合を避ける）
            string hostPort = "11521"; // 別のポートを使用

            Console.WriteLine("Oracleテスト用コンテナを準備しています...");

            // Oracleコンテナの設定 - リストにあるイメージ名とタグを使用
            _oracleContainer = new DockerContainerHelper("gvenzl/oracle-xe:latest")
                .WithName(containerName)
                .WithEnvironment("ORACLE_PASSWORD", "password")
                .WithEnvironment("APP_USER", "testuser")
                .WithEnvironment("APP_USER_PASSWORD", "testpass")
                .WithPortMapping(hostPort, "1521/tcp");

            await _oracleContainer.StartAsync();

            // 動的に割り当てられたポートを使用
            _connectionString = $"User Id=testuser;Password=testpass;Data Source=localhost:{hostPort}/XEPDB1";

            Console.WriteLine("Oracle接続文字列: " + _connectionString);

            // Oracleコンテナは起動に時間がかかるため、十分な待機時間を設定
            Console.WriteLine("Oracleサービスの準備ができるまで待機しています（約30秒）...");
            await Task.Delay(TimeSpan.FromSeconds(30));

            // リポジトリの初期化とテーブルの作成
            _repository = new OracleCustomerRepository(_connectionString);
            await _repository.EnsureTableCreatedAsync();
            Console.WriteLine("Oracleテストの準備が完了しました。");
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static async Task ClassCleanup()
        {
            // テスト終了後にコンテナを停止
            if (_oracleContainer != null)
            {
                await _oracleContainer.StopAsync();
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