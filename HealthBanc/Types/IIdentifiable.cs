using System;

namespace HealthBanc.Types
{
    public interface IIdentifiable
    {
         Guid Id { get; }
    }
}