﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TableDependency.Enums;
using TableDependency.EventArgs;
using TableDependency.SqlClient.Test.Base;

namespace TableDependency.SqlClient.Test.IntegrationTests
{
    [TestClass]
    public class DataAnnotationTest09 : SqlTableDependencyBaseTest
    {
        [Table("XXXX", Schema = "YYYY")]
        private class DataAnnotationTestSqlServer9Model
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        private static readonly string SchemaName = "dbo"; 
        private static readonly string TableName = typeof(DataAnnotationTest09).Name;
        private static int _counter;
        private static Dictionary<string, Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>> _checkValues = new Dictionary<string, Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>>();
        private static Dictionary<string, Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>> _checkValuesOld = new Dictionary<string, Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>>();

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            using (var sqlConnection = new SqlConnection(ConnectionStringForTestUser))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();

                    sqlCommand.CommandText = $"CREATE TABLE [{TableName}]([Id] [int] IDENTITY(1, 1) NOT NULL, [Name] [NVARCHAR](50) NULL, [Long Description] [NVARCHAR](50) NULL)";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            using (var sqlConnection = new SqlConnection(ConnectionStringForTestUser))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"DELETE FROM [{TableName}];";
                    sqlCommand.ExecuteNonQuery();
                }
            }

            _checkValues.Clear();
            _checkValuesOld.Clear();

            _counter = 0;

            _checkValues.Add(ChangeType.Insert.ToString(), new Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>(new DataAnnotationTestSqlServer9Model { Name = "Christian", Description = "Del Bianco" }, new DataAnnotationTestSqlServer9Model()));
            _checkValues.Add(ChangeType.Update.ToString(), new Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>(new DataAnnotationTestSqlServer9Model { Name = "Velia", Description = "Ceccarelli" }, new DataAnnotationTestSqlServer9Model()));
            _checkValues.Add(ChangeType.Delete.ToString(), new Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>(new DataAnnotationTestSqlServer9Model { Name = "Velia", Description = "Ceccarelli" }, new DataAnnotationTestSqlServer9Model()));

            _checkValuesOld.Add(ChangeType.Insert.ToString(), new Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>(new DataAnnotationTestSqlServer9Model { Name = "Christian", Description = "Del Bianco" }, new DataAnnotationTestSqlServer9Model()));
            _checkValuesOld.Add(ChangeType.Update.ToString(), new Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>(new DataAnnotationTestSqlServer9Model { Name = "Velia", Description = "Ceccarelli" }, new DataAnnotationTestSqlServer9Model()));
            _checkValuesOld.Add(ChangeType.Delete.ToString(), new Tuple<DataAnnotationTestSqlServer9Model, DataAnnotationTestSqlServer9Model>(new DataAnnotationTestSqlServer9Model { Name = "Velia", Description = "Ceccarelli" }, new DataAnnotationTestSqlServer9Model()));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var sqlConnection = new SqlConnection(ConnectionStringForTestUser))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestCategory("SqlServer")]
        [TestMethod]
        public void Test()
        {
            SqlTableDependency<DataAnnotationTestSqlServer9Model> tableDependency = null;
            string naming;

            try
            {
                var mapper = new ModelToTableMapper<DataAnnotationTestSqlServer9Model>();
                mapper.AddMapping(c => c.Description, "Long Description");

                tableDependency = new SqlTableDependency<DataAnnotationTestSqlServer9Model>(
                    ConnectionStringForTestUser,
                    tableName: TableName,
                    schemaName: SchemaName,
                    mapper: mapper);

                tableDependency.OnChanged += TableDependency_Changed;
                tableDependency.Start();
                naming = tableDependency.DataBaseObjectsNamingConvention;

                Thread.Sleep(5000);

                var t = new Task(ModifyTableContent);
                t.Start();
                Thread.Sleep(1000 * 15 * 1);
            }
            finally
            {
                tableDependency?.Dispose();
            }

            Assert.AreEqual(_counter, 3);

            Assert.AreEqual(_checkValues[ChangeType.Insert.ToString()].Item2.Name, _checkValues[ChangeType.Insert.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Insert.ToString()].Item2.Description, _checkValues[ChangeType.Insert.ToString()].Item1.Description);
            Assert.IsNull(_checkValuesOld[ChangeType.Insert.ToString()]);

            Assert.AreEqual(_checkValues[ChangeType.Update.ToString()].Item2.Name, _checkValues[ChangeType.Update.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Update.ToString()].Item2.Description, _checkValues[ChangeType.Update.ToString()].Item1.Description);
            Assert.IsNull(_checkValuesOld[ChangeType.Update.ToString()]);

            Assert.AreEqual(_checkValues[ChangeType.Delete.ToString()].Item2.Name, _checkValues[ChangeType.Delete.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Delete.ToString()].Item2.Description, _checkValues[ChangeType.Delete.ToString()].Item1.Description);
            Assert.IsNull(_checkValuesOld[ChangeType.Delete.ToString()]);

            Assert.IsTrue(base.AreAllDbObjectDisposed(naming));
            Assert.IsTrue(base.CountConversationEndpoints(naming) == 0);
        }

        [TestCategory("SqlServer")]
        [TestMethod]
        public void TestWithOldValues()
        {
            SqlTableDependency<DataAnnotationTestSqlServer9Model> tableDependency = null;
            string naming;

            try
            {
                var mapper = new ModelToTableMapper<DataAnnotationTestSqlServer9Model>();
                mapper.AddMapping(c => c.Description, "Long Description");

                tableDependency = new SqlTableDependency<DataAnnotationTestSqlServer9Model>(
                    ConnectionStringForTestUser,
                    includeOldValues: true,
                    tableName: TableName,
                    schemaName: SchemaName,
                    mapper: mapper);

                tableDependency.OnChanged += TableDependency_Changed;
                tableDependency.Start();
                naming = tableDependency.DataBaseObjectsNamingConvention;

                Thread.Sleep(5000);

                var t = new Task(ModifyTableContent);
                t.Start();
                Thread.Sleep(1000 * 15 * 1);
            }
            finally
            {
                tableDependency?.Dispose();
            }

            Assert.AreEqual(_counter, 3);

            Assert.AreEqual(_checkValues[ChangeType.Insert.ToString()].Item2.Name, _checkValues[ChangeType.Insert.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Insert.ToString()].Item2.Description, _checkValues[ChangeType.Insert.ToString()].Item1.Description);
            Assert.IsNull(_checkValuesOld[ChangeType.Insert.ToString()]);

            Assert.AreEqual(_checkValues[ChangeType.Update.ToString()].Item2.Name, _checkValues[ChangeType.Update.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Update.ToString()].Item2.Description, _checkValues[ChangeType.Update.ToString()].Item1.Description);
            Assert.AreEqual(_checkValuesOld[ChangeType.Update.ToString()].Item2.Name, _checkValues[ChangeType.Insert.ToString()].Item2.Name);
            Assert.AreEqual(_checkValuesOld[ChangeType.Update.ToString()].Item2.Description, _checkValues[ChangeType.Insert.ToString()].Item2.Description);

            Assert.AreEqual(_checkValues[ChangeType.Delete.ToString()].Item2.Name, _checkValues[ChangeType.Delete.ToString()].Item1.Name);
            Assert.AreEqual(_checkValues[ChangeType.Delete.ToString()].Item2.Description, _checkValues[ChangeType.Delete.ToString()].Item1.Description);
            Assert.IsNull(_checkValuesOld[ChangeType.Delete.ToString()]);

            Assert.IsTrue(base.AreAllDbObjectDisposed(naming));
            Assert.IsTrue(base.CountConversationEndpoints(naming) == 0);
        }

        private static void TableDependency_Changed(object sender, RecordChangedEventArgs<DataAnnotationTestSqlServer9Model> e)
        {
            _counter++;

            switch (e.ChangeType)
            {
                case ChangeType.Insert:
                    _checkValues[ChangeType.Insert.ToString()].Item2.Name = e.Entity.Name;
                    _checkValues[ChangeType.Insert.ToString()].Item2.Description = e.Entity.Description;

                    if (e.EntityOldValues != null)
                    {
                        _checkValuesOld[ChangeType.Insert.ToString()].Item2.Name = e.EntityOldValues.Name;
                        _checkValuesOld[ChangeType.Insert.ToString()].Item2.Description = e.EntityOldValues.Description;
                    }
                    else
                    {
                        _checkValuesOld[ChangeType.Insert.ToString()] = null;
                    }

                    break;

                case ChangeType.Update:
                    _checkValues[ChangeType.Update.ToString()].Item2.Name = e.Entity.Name;
                    _checkValues[ChangeType.Update.ToString()].Item2.Description = e.Entity.Description;

                    if (e.EntityOldValues != null)
                    {
                        _checkValuesOld[ChangeType.Update.ToString()].Item2.Name = e.EntityOldValues.Name;
                        _checkValuesOld[ChangeType.Update.ToString()].Item2.Description = e.EntityOldValues.Description;
                    }
                    else
                    {
                        _checkValuesOld[ChangeType.Update.ToString()] = null;
                    }

                    break;

                case ChangeType.Delete:
                    _checkValues[ChangeType.Delete.ToString()].Item2.Name = e.Entity.Name;
                    _checkValues[ChangeType.Delete.ToString()].Item2.Description = e.Entity.Description;

                    if (e.EntityOldValues != null)
                    {
                        _checkValuesOld[ChangeType.Delete.ToString()].Item2.Name = e.EntityOldValues.Name;
                        _checkValuesOld[ChangeType.Delete.ToString()].Item2.Description = e.EntityOldValues.Description;
                    }
                    else
                    {
                        _checkValuesOld[ChangeType.Delete.ToString()] = null;
                    }

                    break;
            }
        }

        private static void ModifyTableContent()
        {
            using (var sqlConnection = new SqlConnection(ConnectionStringForTestUser))
            {
                sqlConnection.Open();

                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"INSERT INTO [{TableName}] ([Name], [Long Description]) VALUES ('{_checkValues[ChangeType.Insert.ToString()].Item1.Name}', '{_checkValues[ChangeType.Insert.ToString()].Item1.Description}')";
                    sqlCommand.ExecuteNonQuery();
                }

                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"UPDATE [{TableName}] SET [Name] = '{_checkValues[ChangeType.Update.ToString()].Item1.Name}', [Long Description] = '{_checkValues[ChangeType.Update.ToString()].Item1.Description}'";
                    sqlCommand.ExecuteNonQuery();
                }

                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"DELETE FROM [{TableName}]";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
    }
}