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
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Oracleではシーケンスを使うのが一般的
                string sql = @"
                    INSERT INTO CUSTOMERS (NAME, EMAIL, CREATED_AT) 
                    VALUES (:Name, :Email, :CreatedAt) 
                    RETURNING ID INTO :Id";

                using (OracleCommand command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add("Name", OracleDbType.Varchar2).Value = customer.Name;
                    command.Parameters.Add("Email", OracleDbType.Varchar2).Value = customer.Email;
                    command.Parameters.Add("CreatedAt", OracleDbType.Date).Value = customer.CreatedAt;

                    // OUTパラメータの設定
                    OracleParameter idParam = new OracleParameter("Id", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(idParam);

                    await command.ExecuteNonQueryAsync();

                    // 生成されたIDを取得
                    customer.Id = Convert.ToInt32(idParam.Value.ToString());
                    return customer.Id;
                }
            }
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT ID, NAME, EMAIL, CREATED_AT FROM CUSTOMERS WHERE ID = :Id";

                using (OracleCommand command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add("Id", OracleDbType.Int32).Value = id;

                    using (OracleDataReader reader = await command.ExecuteReaderAsync())
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

            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT ID, NAME, EMAIL, CREATED_AT FROM CUSTOMERS";

                using (OracleCommand command = new OracleCommand(sql, connection))
                using (OracleDataReader reader = await command.ExecuteReaderAsync())
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
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    UPDATE CUSTOMERS 
                    SET NAME = :Name, EMAIL = :Email 
                    WHERE ID = :Id";

                using (OracleCommand command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add("Name", OracleDbType.Varchar2).Value = customer.Name;
                    command.Parameters.Add("Email", OracleDbType.Varchar2).Value = customer.Email;
                    command.Parameters.Add("Id", OracleDbType.Int32).Value = customer.Id;

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "DELETE FROM CUSTOMERS WHERE ID = :Id";

                using (OracleCommand command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add("Id", OracleDbType.Int32).Value = id;

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        // テーブル作成用メソッド（初期化用）
        public async Task EnsureTableCreatedAsync()
        {
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                // シーケンス作成
                string createSeqSql = @"
                    DECLARE
                        seq_exists NUMBER;
                    BEGIN
                        SELECT COUNT(*) INTO seq_exists FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'CUSTOMERS_SEQ';
                        IF seq_exists = 0 THEN
                            EXECUTE IMMEDIATE 'CREATE SEQUENCE CUSTOMERS_SEQ START WITH 1 INCREMENT BY 1';
                        END IF;
                    END;";

                using (OracleCommand seqCommand = new OracleCommand(createSeqSql, connection))
                {
                    await seqCommand.ExecuteNonQueryAsync();
                }

                // テーブル作成
                string createTableSql = @"
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
                    END;";

                using (OracleCommand tableCommand = new OracleCommand(createTableSql, connection))
                {
                    await tableCommand.ExecuteNonQueryAsync();
                }
            }
        }
    }
}