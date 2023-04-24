using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        void Create(T entity);
        void Update(T entity);
        Task<int> Save();
        void Delete(T entity);
        IEnumerable<T> Filter(Func<T, bool> predicate);
        Task<T> Find(Expression<Func<T, bool>> predicate);
        void CreateRange(List<T> entity);
        void DeleteRange(List<T> entity);
        void UpdateRange(List<T> entity);
        IQueryable<T> Query(Func<T, bool> predicate);
        IQueryable<T> QueryAll();
    }
}
