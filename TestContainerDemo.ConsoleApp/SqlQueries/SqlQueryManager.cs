using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;

namespace TestContainerDemo.ConsoleApp.SqlQueries
{
    public static class SqlQueryManager
    {
        private static readonly Dictionary<string, string> _sqlQueries = new Dictionary<string, string>();

        static SqlQueryManager()
        {
            LoadSqlQueries();
        }

        private static void LoadSqlQueries()
        {
            try
            {
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                string xmlPath = Path.Combine(assemblyDirectory, "SqlQueries", "CustomerQueries.xml");

                if (!File.Exists(xmlPath))
                {
                    throw new FileNotFoundException($"SQLクエリXMLファイルが見つかりません: {xmlPath}");
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(xmlPath);

                XmlNodeList queryNodes = doc.SelectNodes("//Query");
                if (queryNodes != null)
                {
                    foreach (XmlNode queryNode in queryNodes)
                    {
                        XmlAttribute idAttribute = queryNode.Attributes["Id"];
                        if (idAttribute != null && !string.IsNullOrEmpty(idAttribute.Value))
                        {
                            string queryId = idAttribute.Value;
                            string sqlText = queryNode.InnerText.Trim();

                            if (!_sqlQueries.ContainsKey(queryId))
                            {
                                _sqlQueries.Add(queryId, sqlText);
                            }
                            else
                            {
                                Console.WriteLine($"警告: クエリID '{queryId}' が重複しています。最初の定義が使用されます。");
                            }
                        }
                    }
                }

                Console.WriteLine($"{_sqlQueries.Count}件のSQLクエリを読み込みました。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQLクエリの読み込み中にエラーが発生しました: {ex.Message}");
                throw;
            }
        }

        public static string GetQuery(string queryId)
        {
            if (_sqlQueries.TryGetValue(queryId, out string query))
            {
                return query;
            }
            throw new KeyNotFoundException($"指定されたID '{queryId}' のSQLクエリが見つかりません。");
        }
    }
}
