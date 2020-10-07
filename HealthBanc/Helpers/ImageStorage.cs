using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Helpers
{
    public class Image
    {
        public ImageStorage ImageStorage { get; set; }
        
    }
    public class ImageStorage
    {
        public string AzureConnectionString { get; set; }
    }
}
