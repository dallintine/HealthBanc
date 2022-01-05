using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess
{
    public class PaginationQuery
    {
        public PaginationQuery()
        {
            PageNumber = 1;
            PageSize = 15;
            SortBy = 0;
        }
        public PaginationQuery(int pageNumber,int pageSize,string searchText,int sortBy)
        {
            PageNumber = pageNumber;
            //PageSize = pageSize > 100 ? 20 : pageSize;
            PageSize = 15;
            SearchText = searchText;
            SortBy = sortBy;
        }

        public int PageNumber { get; set; }
        public int _pageSize;
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                _pageSize = 15;
            }
        }
        public string SearchText { get; set; }
        public int SortBy { get; set; }
        public int? Filter { get; set; }
        public int? Status { get; set; }
    }
}
