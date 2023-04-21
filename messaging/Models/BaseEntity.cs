using System;

namespace messaging.Models
{
  public class BaseEntity{
      // timestamps are in UTC
      public DateTime CreatedDate { get; set; }
      public DateTime UpdatedDate { get; set; }
  }
}
