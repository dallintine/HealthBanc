using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Implementations
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        public BaseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<T> GetAll()
        {
            return _context.Set<T>();
        }

        public async Task<int> Save()
        {
            return await _context.SaveChangesAsync();
        }

        public void Create(T entity)
        {
            _context.Add(entity);
        }

        public void CreateRange(List<T> entity)
        {
            _context.AddRange(entity);
        }

        public void Delete(T entity)
        {
            _context.Remove(entity);
        }

        public void DeleteRange(List<T> entity)
        {
            _context.RemoveRange(entity);
        }

        public IQueryable<T> Query(Func<T, bool> predicate)
        {
            return _context.Set<T>().Where(predicate).AsQueryable();
        }

        public IQueryable<T> QueryAll()
        {
            return _context.Set<T>().AsQueryable();
        }

        public IEnumerable<T> Filter(Func<T, bool> predicate)
        {
            return _context.Set<T>().Where(predicate);
        }

        public async Task<T> Find(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public void Update(T entity)
        {
            _context.Update(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void UpdateRange(List<T> entity)
        {
            _context.UpdateRange(entity);
        }
    }
}
