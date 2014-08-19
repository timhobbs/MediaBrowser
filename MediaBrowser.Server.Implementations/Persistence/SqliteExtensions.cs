﻿using MediaBrowser.Model.Logging;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Server.Implementations.Persistence
{
    /// <summary>
    /// Class SQLiteExtensions
    /// </summary>
    static class SqliteExtensions
    {
        /// <summary>
        /// Determines whether the specified conn is open.
        /// </summary>
        /// <param name="conn">The conn.</param>
        /// <returns><c>true</c> if the specified conn is open; otherwise, <c>false</c>.</returns>
        public static bool IsOpen(this IDbConnection conn)
        {
            return conn.State == ConnectionState.Open;
        }

        public static IDataParameter GetParameter(this IDbCommand cmd, int index)
        {
            return (IDataParameter)cmd.Parameters[index];
        }
        
        public static IDataParameter Add(this IDataParameterCollection paramCollection, IDbCommand cmd, string name, DbType type)
        {
            var param = cmd.CreateParameter();
           
            param.ParameterName = name;
            param.DbType = type;

            paramCollection.Add(param);

            return param;
        }

        public static IDataParameter Add(this IDataParameterCollection paramCollection, IDbCommand cmd, string name)
        {
            var param = cmd.CreateParameter();

            param.ParameterName = name;

            paramCollection.Add(param);
            
            return param;
        }

        
        /// <summary>
        /// Gets a stream from a DataReader at a given ordinal
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="ordinal">The ordinal.</param>
        /// <returns>Stream.</returns>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public static Stream GetMemoryStream(this IDataReader reader, int ordinal)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            var memoryStream = new MemoryStream();
            var num = 0L;
            var array = new byte[4096];
            long bytes;
            do
            {
                bytes = reader.GetBytes(ordinal, num, array, 0, array.Length);
                memoryStream.Write(array, 0, (int)bytes);
                num += bytes;
            }
            while (bytes > 0L);
            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// Runs the queries.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="queries">The queries.</param>
        /// <param name="logger">The logger.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        /// <exception cref="System.ArgumentNullException">queries</exception>
        public static void RunQueries(this IDbConnection connection, string[] queries, ILogger logger)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }

            using (var tran = connection.BeginTransaction())
            {
                try
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        foreach (var query in queries)
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = query;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
                catch (Exception e)
                {
                    logger.ErrorException("Error running queries", e);
                    tran.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Connects to db.
        /// </summary>
        /// <param name="dbPath">The db path.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Task{IDbConnection}.</returns>
        /// <exception cref="System.ArgumentNullException">dbPath</exception>
        public static async Task<IDbConnection> ConnectToDb(string dbPath, ILogger logger)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentNullException("dbPath");
            }

            logger.Info("Opening {0}", dbPath);

            var connectionstr = new SQLiteConnectionStringBuilder
            {
                PageSize = 4096,
                CacheSize = 4096,
                SyncMode = SynchronizationModes.Normal,
                DataSource = dbPath,
                JournalMode = SQLiteJournalModeEnum.Wal
            };

            var connection = new SQLiteConnection(connectionstr.ConnectionString);

            await connection.OpenAsync().ConfigureAwait(false);

            return connection;
        }

        /// <summary>
        /// Serializes to bytes.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Byte[][].</returns>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public static byte[] SerializeToBytes(this IJsonSerializer json, object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            using (var stream = new MemoryStream())
            {
                json.SerializeToStream(obj, stream);
                return stream.ToArray();
            }
        }
    }
}