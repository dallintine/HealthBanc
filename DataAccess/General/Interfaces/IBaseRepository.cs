using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.General.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        void Create(T entity);
        void Update(T entity);
        Task<int> Save();
        void Delete(T entity);
        IEnumerable<T> Filter(Func<T, bool> predicate);
        T Find(Func<T, bool> predicate);
        void CreateRange(List<T> entity);
    }
}
