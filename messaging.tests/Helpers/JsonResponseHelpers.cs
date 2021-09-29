using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System.Net.Http.Headers;
using Hl7.Fhir.ElementModel;
using System;

namespace messaging.tests.Helpers
{
  public class JsonResponseHelpers
  {
    public static async Task<JObject> GetResultAsync(HttpResponseMessage response) {
      var content = await response.Content.ReadAsStringAsync();
      return JObject.Parse(content);
    }

    public static async Task<Bundle> ParseBundleAsync(HttpResponseMessage response) {
      var content = await response.Content.ReadAsStringAsync();
      FhirJsonParser parser = new FhirJsonParser();
      return parser.Parse<Bundle>(content);
    }

    public static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string endpoint, string jsonBundle) {
      HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
      postRequest.Content = new StringContent(jsonBundle);
      postRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      return await client.SendAsync(postRequest);
    }
  }
}
