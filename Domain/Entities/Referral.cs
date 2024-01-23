using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Referral : BaseEntity
    {
        public long ParentUserId { get; set; }
        [ForeignKey("ParentUserId")]
        public ApplicationUser ParentUser { get; set; }
        public long ChildUserId { get; set; }
        [ForeignKey("ChildUserId")]
        public ApplicationUser ChildUser { get; set; }
    }
}
