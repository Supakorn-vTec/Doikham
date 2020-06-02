using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Doikham.API
{
    public interface IDBHelper
    {
        public Task<DataTable> ExecuteReaderAsync(string queryString, string connectionString);
        public Task<DataTable> ExecuteReaderAsync(string queryString, SqlConnection connection, SqlTransaction transaction);
        public Task<int> ExecuteNonQuery(string queryString, string connectionString);
        public Task<int> ExecuteNonQuery(string queryString, SqlConnection connection, SqlTransaction transaction);
    }

    public class SqlServer : IDBHelper
    {
        public async Task<DataTable> ExecuteReaderAsync(string queryString, string connectionString)
        {
            DataTable dt = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = await command.ExecuteReaderAsync();

                dt.Load(reader);
            }
            return dt;
        }
        public async Task<DataTable> ExecuteReaderAsync(string queryString,  SqlConnection connection, SqlTransaction transaction)
        {
            DataTable dt = new DataTable();
            SqlCommand command = new SqlCommand(queryString, connection);
            command.Connection = connection;
            command.Transaction = transaction;
            SqlDataReader reader = await command.ExecuteReaderAsync();

            dt.Load(reader);
            return dt;
        }
        public async Task<int> ExecuteNonQuery(string queryString, string connectionString)
        {
            int id = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                id = await command.ExecuteNonQueryAsync();
            }
            return id;
        }
        public async Task<int> ExecuteNonQuery(string queryString, SqlConnection connection, SqlTransaction transaction)
        {
            int id = 0;
            SqlCommand command = new SqlCommand(queryString, connection);
            command.Connection = connection;
            command.Transaction = transaction;
            id = await command.ExecuteNonQueryAsync();
            return id;
        }
    }
}
