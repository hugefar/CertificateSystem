using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CertificateSystem.DBUtility
{
    public static class SqlHelper
    {
        private static string? _connectionString;

        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string is null or empty. Provide a valid connection string when initializing SqlHelper.");
            }
        }

        private static SqlConnection GetConnection()
        {
            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException("SqlHelper is not initialized. Call SqlHelper.Initialize(configuration) at startup.");

            return new SqlConnection(_connectionString);
        }

        public static SqlConnection CreateConnection()
        {
            return GetConnection();
        }

        public static int ExecuteNonQuery(string sql, CommandType cmdType, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn) { CommandType = cmdType };
            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string sql, CommandType cmdType, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn) { CommandType = cmdType };
            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteScalar();
        }

        public static DataTable ExecuteDataTable(string sql, CommandType cmdType, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn) { CommandType = cmdType };
            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public static SqlDataReader ExecuteReader(string sql, CommandType cmdType, params SqlParameter[] parameters)
        {
            var conn = GetConnection();
            var cmd = new SqlCommand(sql, conn) { CommandType = cmdType };
            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);
            conn.Open();
            // CommandBehavior.CloseConnection ensures connection is closed when reader is closed
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }
    }
}
