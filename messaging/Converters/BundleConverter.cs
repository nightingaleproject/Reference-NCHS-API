using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace messaging
{
  public class BundleConverter : JsonConverter<Bundle>
  {
    public override Bundle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, Bundle value, JsonSerializerOptions options)
    {
      writer.WriteRawValue(value.ToJson());
    }
  }
}
