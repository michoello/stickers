using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace API
{
    // TODO: split this file, one class per file
    // TODO: remove Enum hacks

    // Wrapper class on top of Azure Table storage
    // Benefits:
    // 1. It can store any custom object, Enums, generic Lists, Dictionaries in the Azure table
    // 2. It is able to generate unique RowKey for its entities using Blob helper
    public class AzureTable
    {
        // A wrapper for standard TableEntity
        // So far Azure does not support serialization for Enums and nontrivial members of entity
        // This class fills that functionality gap.
        public class Entity : TableEntity
        {
            // TODO: do not serialize null values (?)
            public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
            {
                IDictionary<string, EntityProperty> items = base.WriteEntity(operationContext);

                foreach (PropertyInfo pi in
                    this.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                        .Where(pi => !items.ContainsKey(pi.Name)))
                {
                    if (pi.PropertyType.IsEnum)
                    {
                        // Little hack for enums: they are stored exactly as strings are, without parentheses
                        items.Add(pi.Name, new EntityProperty(pi.GetValue(this).ToString()));
                    }
                    else
                    {
                        items.Add(pi.Name, new EntityProperty(pi.GetValue(this).ToJson()));
                    }
                }
                return items;
            }

            public override void ReadEntity(IDictionary<string, EntityProperty> items, OperationContext operationContext)
            {
                base.ReadEntity(items, operationContext);

                foreach (string key in items.Keys)
                {
                    PropertyInfo pi = this.GetType().GetProperty(key);

                    if (pi != null && pi.PropertyType != typeof(String) && items[key].PropertyType == EdmType.String) 
                    {
                        pi.SetValue(this, Activator.CreateInstance(pi.PropertyType));
                        try
                        {
                            pi.SetValue(this, pi.GetValue(this).FromJson(items[key].StringValue));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Got exception while unjson field " + pi.Name + " value " + items[key].StringValue, e.GetType());
                            //throw new StorageException("Got exception while unjson field " + pi.Name + " value " + items[key].StringValue, e);
                        }
                    }
                }
            }
        }

        protected CloudTable cloudTable;
        //protected CloudBlockBlob counterBlob;
        public UniqueId IdCounter;

        public string Name { get; private set; }

        public AzureTable(string tableName, string connectionString)
        {
            Name = tableName;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create content table
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            cloudTable = tableClient.GetTableReference(Name);
            cloudTable.CreateIfNotExists();

            // Create counter for unique RowKey generation
            IdCounter = new UniqueId(Name + ".RowKey", connectionString);
        }
    };

    //
    // Generic wrapper which makes azure table strong typed
    // Only claimed T type entities can be stored using its interfaces
    //
    public class AzureTable<T> : AzureTable where T : AzureTable.Entity, new()
    {
        public AzureTable(string tableName, string connectionString)
            : base(tableName, connectionString)
        {
        }

        public void Insert(T entity)
        {
            entity.PartitionKey = entity.PartitionKey ?? "default";
            entity.RowKey = entity.RowKey ?? IdCounter.GetAndSetNext().ToString();

            cloudTable.Execute(TableOperation.Insert(entity));
        }

        public IEnumerable<T> Select(string where = null, int take = -1)
        {
            var query = new TableQuery<T>();
            if (where != null && where != "") query = query.Where(where);
            if (take != -1) query = query.Take(take);

            return cloudTable.ExecuteQuery(query);
        }

        public void Update(T entity)
        {
            cloudTable.Execute(TableOperation.Replace(entity));
        }

        public void UpSert(T entity)
        {
            cloudTable.Execute(TableOperation.InsertOrReplace(entity));
        }

        public void Delete(T entity)
        {
            cloudTable.Execute(TableOperation.Delete(entity));
        }
    };
}
