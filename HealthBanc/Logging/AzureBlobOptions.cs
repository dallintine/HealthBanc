using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Logging
{
    public class AzureBlobOptions
    {
        public bool Enabled { get; set; }
        public string ConnectionString { get; set; }
        public string StorageContainerName { get; set; }
        public string StorageFileName { get; set; }
        public string OutputTemplate { get; set; }
    }
}
