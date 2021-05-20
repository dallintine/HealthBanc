using DataAccess.General.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IPaymentReferenceRepository : IBaseRepository<PaymentReference>
    {
        Task<PaymentReference> GetByUserId(int userId);
        Task<PaymentReference> GetById(int id);
        Task<PaymentReference> GetByReference(string reference);
        Task<PagedResponse<PaymentReference>> GetPaginatedPaymentReference(PaginationQuery paginationQuery,string email);
        IQueryable<PaymentReference> QueryAllPaymentReference();
        Task<List<PaymentReference>> CorrectPaymentReference(DateTime date);
    }
}
