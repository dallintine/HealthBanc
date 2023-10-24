using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IFileService
    {
        byte[] GenerateGenericExcelFile<T>(List<T> sample, string sheetName);
    }
}
