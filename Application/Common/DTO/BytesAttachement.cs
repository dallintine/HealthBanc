using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTO
{
    public class BytesAttachement
    {
        public string FileName { get; set; }

        public byte[] Attachmentfile { get; set; }
    }
}
