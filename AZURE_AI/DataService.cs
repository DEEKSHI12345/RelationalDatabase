using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AZURE_AI
{
    public static class DataService
    {
        public static List<TableSchema> GetDatabaseSchema()
        {
            var tables = new List<TableSchema>();

            using (SqlConnection connection = new SqlConnection("Server=SPCOKLAP-5527\\SQLEXPRESS;Database=Project_Management_System;Trusted_Connection=True;TrustServerCertificate=True;"))
            {
                connection.Open();
                string query = @"
                    SELECT 
                        TABLE_NAME, 
                        COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    ORDER BY TABLE_NAME, ORDINAL_POSITION";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        TableSchema currentTable = null;

                        while (reader.Read())
                        {
                            string tableName = reader["TABLE_NAME"].ToString();
                            string columnName = reader["COLUMN_NAME"].ToString();

                            if (currentTable == null || currentTable.TableName != tableName)
                            {
                                if (currentTable != null)
                                {
                                    tables.Add(currentTable);
                                }

                                currentTable = new TableSchema
                                {
                                    TableName = tableName,
                                    Columns = new List<string>()
                                };
                            }

                            currentTable.Columns.Add(columnName);
                        }

                        if (currentTable != null)
                        {
                            tables.Add(currentTable);
                        }
                    }
                }
            }

            return tables;
        }

        public static List<List<string>> GetTable(string query)
        {
            var rows = new List<List<string>>();

            using (SqlConnection connection = new SqlConnection("Server=SPCOKLAP-5527\\SQLEXPRESS;Database=Project_Management_System;Trusted_Connection=True;TrustServerCertificate=True;"))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        bool headerAdded = false;
                        while (reader.Read())
                        {
                            var cols = new List<string>();
                            var headerCols = new List<string>();
                            if (!headerAdded)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    headerCols.Add(reader.GetName(i));
                                }
                                headerAdded = true;
                                rows.Add(headerCols);
                            }
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                try
                                {
                                    cols.Add(reader.GetValue(i).ToString());
                                }
                                catch
                                {
                                    cols.Add("DataTypeConversionError");
                                }
                            }
                            rows.Add(cols);
                        }
                    }
                    return rows;
                }
            }
        }
        public static List<(string Table, List<string> Columns)> ParseQuery(string query)
        {
            var results = new List<(string Table, List<string> Columns)>();

            var tableRegex = new Regex(@"\bFROM\s+([^\s,]+)|\bJOIN\s+([^\s,]+)", RegexOptions.IgnoreCase);
            var columnRegex = new Regex(@"\bSELECT\s+(.*?)\bFROM\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var tableMatches = tableRegex.Matches(query);
            var columnMatches = columnRegex.Match(query);

            var tables = new HashSet<string>();
            foreach (Match match in tableMatches)
            {
                if (match.Groups[1].Success)
                {
                    tables.Add(match.Groups[1].Value);
                }
                else if (match.Groups[2].Success)
                {
                    tables.Add(match.Groups[2].Value);
                }
            }

            var columns = new List<string>();
            if (columnMatches.Success)
            {
                var columnPart = columnMatches.Groups[1].Value;
                var columnNames = columnPart.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var columnName in columnNames)
                {
                    columns.Add(columnName.Trim());
                }
            }

            foreach (var table in tables)
            {
                results.Add((table, columns));
            }

            return results;
        }

    }
    public class TableSchema
    {
        public string TableName { get; set; }
        public List<string> Columns { get; set; }
    }
}
