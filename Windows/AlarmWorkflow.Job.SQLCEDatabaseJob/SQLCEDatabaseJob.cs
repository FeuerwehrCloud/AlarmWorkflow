﻿using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Shared.Diagnostics;
using AlarmWorkflow.Shared.Extensibility;

namespace AlarmWorkflow.Job.SQLCEDatabaseJob
{
    class SQLCEDatabaseJob : IJob, IOperationStore
    {
        #region Fields

        private readonly object Lock = new object();

        #endregion

        #region IJob Member

        bool IJob.DoJob(Operation operation)
        {
            try
            {
                lock (Lock)
                {
                    using (SQLCEDatabaseEntities entities = this.CreateContext<SQLCEDatabaseEntities>())
                    {
                        int oid = operation.Id;
                        if (operation.Id == 0)
                        {
                            oid = entities.Operations.Any() ? entities.Operations.Max(o => o.OperationId) + 1 : 1;
                        }
                        OperationData data = new OperationData()
                        {
                            OperationId = oid,
                            Timestamp = DateTime.UtcNow,
                            City = operation.City,
                            ZipCode = operation.ZipCode,
                            Location = operation.Location,
                            OperationNumber = operation.OperationNumber,
                            Keyword = operation.Keyword,
                            Comment = operation.Comment,
                            IsAcknowledged = operation.IsAcknowledged,
                            Messenger = operation.Messenger,
                            Property = operation.Property,
                            Street = operation.Street,
                            StreetNumber = operation.StreetNumber,
                        };
                        entities.Operations.AddObject(data);
                        entities.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogFormat(LogType.Error, this, "An error occurred while trying to write the operation to the database!");
                Logger.Instance.LogException(this, ex);
                return false;
            }

            return true;
        }

        string IJob.ErrorMessage
        {
            get { return ""; }
        }

        void IJob.Initialize()
        {

        }

        #endregion

        #region IOperationStore Member

        int IOperationStore.GetNextOperationId()
        {
            lock (Lock)
            {
                using (SQLCEDatabaseEntities entities = CreateContext<SQLCEDatabaseEntities>())
                {
                    if (entities.Operations.Any())
                    {
                        return entities.Operations.Max(o => o.OperationId) + 1;
                    }
                    return 1;
                }
            }
        }

        void IOperationStore.AcknowledgeOperation(int operationId)
        {
            lock (Lock)
            {
                using (SQLCEDatabaseEntities entities = CreateContext<SQLCEDatabaseEntities>())
                {
                    OperationData data = entities.Operations.FirstOrDefault(d => d.OperationId == operationId);
                    // If either there is no operation by this id, or the operation exists and is already acknowledged, do nothing
                    if (data == null || data.IsAcknowledged)
                    {
                        return;
                    }

                    // Acknowledge this operation and save changes
                    data.IsAcknowledged = true;
                    entities.SaveChanges();
                }
            }
        }

        Operation IOperationStore.GetOperationById(int operationId)
        {
            lock (Lock)
            {
                List<Operation> operations = new List<Operation>();

                using (SQLCEDatabaseEntities entities = CreateContext<SQLCEDatabaseEntities>())
                {
                    OperationData data = entities.Operations.FirstOrDefault(d => d.OperationId == operationId);
                    if (data == null)
                    {
                        return null;
                    }

                    return new Operation()
                    {
                        Id = data.OperationId,
                        Timestamp = data.Timestamp,
                        City = data.City,
                        IsAcknowledged = data.IsAcknowledged,
                        Keyword = data.Keyword,
                        Location = data.Location,
                        Messenger = data.Messenger,
                        OperationNumber = data.OperationNumber,
                        Property = data.Property,
                        Street = data.Street,
                        StreetNumber = data.StreetNumber,
                        ZipCode = data.ZipCode,
                    };
                }
            }
        }

        IList<int> IOperationStore.GetOperationIds(int maxAge, bool onlyNonAcknowledged, int limitAmount)
        {
            lock (Lock)
            {
                List<int> operations = new List<int>();

                using (SQLCEDatabaseEntities entities = CreateContext<SQLCEDatabaseEntities>())
                {
                    foreach (OperationData data in entities.Operations.OrderByDescending(o => o.Timestamp))
                    {
                        // If we only want non-acknowledged ones
                        if (onlyNonAcknowledged && data.IsAcknowledged)
                        {
                            continue;
                        }

                        operations.Add(data.OperationId);

                        // If we need to limit operations
                        if (limitAmount > 0 && operations.Count >= limitAmount)
                        {
                            break;
                        }
                    }
                }

                return operations;
            }
        }

        #endregion

        #region Methods

        private T CreateContext<T>() where T : ObjectContext
        {
            try
            {
                string resourceName = this.GetType().Assembly.GetName().Name + ".app.config";
                using (Stream stream = this.GetType().Assembly.GetManifestResourceStream(resourceName))
                {

                    XDocument appConfig = XDocument.Load(stream);

                    XElement connectionStrings = appConfig.Root.Element("connectionStrings");

                    // get first connection string
                    XElement connectionStringE = connectionStrings.Elements("add").Where(n => n.Attribute("name").Value == "SQLCEDatabaseEntities").FirstOrDefault();

                    string name = connectionStringE.Attribute("name").Value;
                    string connectionString = connectionStringE.Attribute("connectionString").Value;

                    return (T)Activator.CreateInstance(typeof(T), connectionString);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
