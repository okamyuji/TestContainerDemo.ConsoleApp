using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TestContainerDemo.ConsoleApp.Models;

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
                    command.CommandText = @"
                        INSERT INTO CUSTOMERS (NAME, EMAIL, CREATED_AT) 
                        VALUES (:Name, :Email, :CreatedAt) 
                        RETURNING ID INTO :Id";

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
                return await connection.QuerySingleOrDefaultAsync<Customer>(
                    "SELECT ID, NAME, EMAIL, CREATED_AT FROM CUSTOMERS WHERE ID = :Id",
                    new { Id = id });
            }
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                return await connection.QueryAsync<Customer>(
                    "SELECT ID, NAME, EMAIL, CREATED_AT FROM CUSTOMERS");
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                int rowsAffected = await connection.ExecuteAsync(
                    @"UPDATE CUSTOMERS 
                      SET NAME = :Name, EMAIL = :Email 
                      WHERE ID = :Id",
                    customer);

                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                int rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM CUSTOMERS WHERE ID = :Id",
                    new { Id = id });

                return rowsAffected > 0;
            }
        }

        // テーブル作成用メソッド（初期化用）
        public async Task EnsureTableCreatedAsync()
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                // シーケンス作成
                await connection.ExecuteAsync(@"
                    DECLARE
                        seq_exists NUMBER;
                    BEGIN
                        SELECT COUNT(*) INTO seq_exists FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'CUSTOMERS_SEQ';
                        IF seq_exists = 0 THEN
                            EXECUTE IMMEDIATE 'CREATE SEQUENCE CUSTOMERS_SEQ START WITH 1 INCREMENT BY 1';
                        END IF;
                    END;");

                // テーブル作成
                await connection.ExecuteAsync(@"
                    DECLARE
                        table_exists NUMBER;
                    BEGIN
                        SELECT COUNT(*) INTO table_exists FROM USER_TABLES WHERE TABLE_NAME = 'CUSTOMERS';
                        IF table_exists = 0 THEN
                            EXECUTE IMMEDIATE 'CREATE TABLE CUSTOMERS (
                                ID NUMBER PRIMARY KEY,
                                NAME VARCHAR2(100) NOT NULL,
                                EMAIL VARCHAR2(100) UNIQUE NOT NULL,
                                CREATED_AT DATE NOT NULL
                            )';
                            
                            EXECUTE IMMEDIATE 'CREATE OR REPLACE TRIGGER CUSTOMERS_BI_TRG 
                            BEFORE INSERT ON CUSTOMERS 
                            FOR EACH ROW 
                            BEGIN 
                                IF :NEW.ID IS NULL THEN 
                                    SELECT CUSTOMERS_SEQ.NEXTVAL INTO :NEW.ID FROM DUAL; 
                                END IF; 
                            END;';
                        END IF;
                    END;");
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