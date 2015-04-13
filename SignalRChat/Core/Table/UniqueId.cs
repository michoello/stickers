using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using System.IO;

namespace API
{
    public class UniqueId
    {
        string Name;
        CloudBlockBlob counterBlob;

        public UniqueId(string idName, string connectionString)
        {
            Name = idName;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer cntCont = blobClient.GetContainerReference("counters");
            cntCont.CreateIfNotExists();
            counterBlob = cntCont.GetBlockBlobReference(Name);
            if (!counterBlob.Exists())
            {
                counterBlob.UploadFromStream(new MemoryStream(Encoding.UTF8.GetBytes("0")));
            }
        }

        //
        // This function returns a current value of counter stored in Azure blob.
        // The value is guaranteed to be unique, i.e. the value always increments after current value is obtained.
        //
        public long GetAndSetNext(int attempts = 20)
        {
            for (int i = 0, timeout = 100; i < attempts; ++i, timeout *= 2)
            {
                // Optimistic approach: upload will fail if the blob has been updated by somebody else 
                // after we downloaded it
                try
                {
                    long counter = Get();

                    Set(counter + 1,
                        accessCondition: AccessCondition.GenerateIfMatchCondition(counterBlob.Properties.ETag)
                        );

                    return counter;
                }
                catch (StorageException)
                {
                    // one more attempt
                }
                Thread.Sleep(timeout);
            }
            throw new Exception("Too much attempts incrementing counter for table '" + Name + "'. Too high load.");
        }

        public long Get()
        {
            long counter = 0;
            Int64.TryParse(counterBlob.DownloadText(), out counter);
            return counter;
        }

        public void Set(long value, AccessCondition accessCondition = null)
        {
            if (accessCondition != null)
            {
                counterBlob.UploadText(value.ToString(), accessCondition: accessCondition);
            }
            else
            {
                counterBlob.UploadText(value.ToString());
            }
        }
    }
}
