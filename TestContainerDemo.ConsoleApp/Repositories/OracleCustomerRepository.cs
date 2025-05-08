using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TestContainerDemo.ConsoleApp.Models;
using TestContainerDemo.ConsoleApp.SqlQueries;

namespace TestContainerDemo.ConsoleApp.Repositories
{
    public class OracleCustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public OracleCustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> CreateCustomerAsync(Customer customer)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Dapperの代わりに素のADO.NETを使用して、IDを取得する
                // これはOracle特有の方法でRETURNING句と出力パラメータを処理するため
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = SqlQueryManager.GetQuery("Oracle_CreateCustomer");

                    command.Parameters.Add("Name", OracleDbType.Varchar2).Value = customer.Name;
                    command.Parameters.Add("Email", OracleDbType.Varchar2).Value = customer.Email;
                    command.Parameters.Add("CreatedAt", OracleDbType.Date).Value = customer.CreatedAt;

                    var idParam = new OracleParameter("Id", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(idParam);

                    await command.ExecuteNonQueryAsync();

                    customer.Id = Convert.ToInt32(idParam.Value.ToString());
                    return customer.Id;
                }
            }
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Oracle_GetCustomerById");
                return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
            }
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Oracle_GetAllCustomers");
                return await connection.QueryAsync<Customer>(sql);
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Oracle_UpdateCustomer");
                int rowsAffected = await connection.ExecuteAsync(sql, customer);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                var sql = SqlQueryManager.GetQuery("Oracle_DeleteCustomer");
                int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
                return rowsAffected > 0;
            }
        }

        public async Task EnsureTableCreatedAsync()
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                // シーケンス作成
                var createSequenceSql = SqlQueryManager.GetQuery("Oracle_CreateSequence");
                await connection.ExecuteAsync(createSequenceSql);

                // テーブル作成
                var createTableSql = SqlQueryManager.GetQuery("Oracle_CreateTable");
                await connection.ExecuteAsync(createTableSql);
            }
        }
    }

    // Oracle用DynamicParametersクラス (Dapperの拡張)
    public class OracleDynamicParameters : SqlMapper.IDynamicParameters
    {
        private readonly Dictionary<string, OracleParameter> _parameters = new Dictionary<string, OracleParameter>();

        public void Add(string name, object value = null, OracleDbType? dbType = null, ParameterDirection? direction = null, int? size = null)
        {
            var parameter = new OracleParameter
            {
                ParameterName = name,
                Value = value ?? DBNull.Value
            };

            if (dbType.HasValue)
                parameter.OracleDbType = dbType.Value;

            if (direction.HasValue)
                parameter.Direction = direction.Value;

            if (size.HasValue)
                parameter.Size = size.Value;

            _parameters[name] = parameter;
        }

        public T Get<T>(string name)
        {
            var value = _parameters[name].Value;

            // DBNullの場合はデフォルト値を返す
            if (value == null || value == DBNull.Value)
            {
                return default;
            }

            // Numberの場合はIntへの明示的な変換が必要
            if (typeof(T) == typeof(int) && value is decimal)
            {
                return (T)(object)Convert.ToInt32(value);
            }

            // その他の型は標準的な変換を試みる
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            var oracleCommand = (OracleCommand)command;
            foreach (var param in _parameters.Values)
            {
                oracleCommand.Parameters.Add(param);
            }
        }
    }
}