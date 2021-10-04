using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using Newtonsoft.Json;

namespace messaging
{
  public class BundleConverter : JsonConverter<Bundle>
  {
    public override Bundle ReadJson(JsonReader reader, Type objectType, Bundle existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }
    public override void WriteJson(JsonWriter writer, Bundle value, JsonSerializer options)
    {
      value.WriteTo(writer);
    }
  }
}
