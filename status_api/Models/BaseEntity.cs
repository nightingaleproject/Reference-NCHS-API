using System;

namespace status_api.Models
{
  public class BaseEntity{
      // read-only
      // timestamps are in UTC
      public DateTime CreatedDate { get; }
      public DateTime UpdatedDate { get; }
  }
}
