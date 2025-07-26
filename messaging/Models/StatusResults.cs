//using System.Text.Json.Serialization;
using System;

namespace messaging.Models
{
		public class StatusResults
		{
				// Only one or none of these attributes will be non-null
				// depending on the status query
				public string? Source { get; set; }
				public string? EventType { get; set; }
				public string? JurisdictionId { get; set; }

				// These attributes must have a query result or default value
				public int ProcessedCount { get; set; } = 0;
				public int QueuedCount { get; set; } = 0;
				public DateTime OldestQueued { get; set; } = DateTime.MinValue;
				public DateTime NewestQueued { get; set; } = DateTime.MinValue;
				public DateTime LatestProcessed { get; set; } = DateTime.MinValue;
				public int ProcessedCountFiveMinutes { get; set; } = 0;
				public int ProcessedCountOneHour { get; set; } = 0;
				public int QueuedCountFiveMinutes { get; set; } = 0;
				public int QueuedCountOneHour { get; set; } = 0;
		}
}
