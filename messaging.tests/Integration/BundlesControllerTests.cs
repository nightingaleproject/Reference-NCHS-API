using Xunit;
using System.Net.Http;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using messaging.Controllers;
using messaging.tests.Helpers;
using VRDR;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using messaging.Models;

namespace messaging.tests
{
  public class BundlesControllerTests : IClassFixture<CustomWebApplicationFactory<messaging.Startup>>
  {
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<messaging.Startup> _factory;

    private readonly ApplicationDbContext _context;

    public BundlesControllerTests(
        CustomWebApplicationFactory<messaging.Startup> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        IServiceScope serviceScope = factory.Services.GetService<IServiceScopeFactory>().CreateScope();
        _context  = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
    }

    [Fact]
    public async Task NewSubmissionMessagePostCreatesNewAcknowledgement()
    {
      // Get the list of outgoing messages currently in the database for
      // reference later
      HttpResponseMessage bundles = await _client.GetAsync("/Bundles");
      var baseBundle = await JsonResponseHelpers.ParseBundleAsync(bundles);

      // Create a new empty Death Record
      DeathRecordSubmission recordSubmission = new DeathRecordSubmission(new DeathRecord());

      // Submit that Death Record
      HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/Bundles", recordSubmission.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

      Hl7.Fhir.Model.Bundle updatedBundle = null;
      // This code does not have access to the background jobs, the best that can
      // be done is checking to see if the response is correct and if it is still
      // incorrect after the specified delay then assuming that something is wrong
      for(int x = 0; x < 3; ++x) {
        HttpResponseMessage oneAck = await _client.GetAsync("/Bundles");
        updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);
        if(updatedBundle.Entry.Count > baseBundle.Entry.Count) {
          break;
        } else {
          await Task.Delay(x * 500);
        }
      }
      Assert.Equal(baseBundle.Entry.Count + 1, updatedBundle.Entry.Count);

      // Extract the message from the bundle and ensure it is an ACK for the appropritae message
      var lastMessageInBundle = updatedBundle.Entry.Last();
      AckMessage parsedMessage = BaseMessage.Parse<AckMessage>((Hl7.Fhir.Model.Bundle)lastMessageInBundle.Resource);
      Assert.Equal(recordSubmission.MessageId, parsedMessage.AckedMessageId);
    }

    [Fact]
      public async Task UnparsableMessagesCauseAnError() {
      HttpResponseMessage createBrokenSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/Bundles", "{}");
      Assert.Equal(HttpStatusCode.BadRequest, createBrokenSubmissionMessage.StatusCode);
    }

    [Fact]
    public async Task DuplicateSubmissionMessageIsIgnored()
    {
      // Get the list of outgoing messages currently in the database for
      // reference later
      HttpResponseMessage bundles = await _client.GetAsync("/Bundles");
      var baseBundle = await JsonResponseHelpers.ParseBundleAsync(bundles);

      // Get the current size of the number of IJEItems in the database
      var ijeItems = _context.IJEItems.Count();

      // Create a new empty Death Record
      DeathRecordSubmission recordSubmission = new DeathRecordSubmission(new DeathRecord());

      // Submit that Death Record
      HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/Bundles", recordSubmission.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

      // Submit Identifical Death Record Again
      HttpResponseMessage duplicateSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/Bundles", recordSubmission.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, duplicateSubmissionMessage.StatusCode);

      // Since the background task should ignore duplicate messages if working correctly,
      // provide ample time for it to finish.
      await Task.Delay(4000);

      HttpResponseMessage oneAck = await _client.GetAsync("/Bundles");
      Hl7.Fhir.Model.Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);

      // Even though the message is a duplicate, it is still ACK'd
      Assert.Equal(baseBundle.Entry.Count + 2, updatedBundle.Entry.Count);

      // Since the message is a duplicate, only 1 message per ID is actually parsed.
      Assert.Equal(ijeItems + 1, _context.IJEItems.Count());
    }

    [Fact]
    public async Task UpdateMessagesAreSuccessfullyAcknowledged()
    {
      // Get the list of outgoing messages currently in the database for
      // reference later
      HttpResponseMessage bundles = await _client.GetAsync("/Bundles");
      var baseBundle = await JsonResponseHelpers.ParseBundleAsync(bundles);

      // Get the current size of the number of IJEItems in the database
      var ijeItems = _context.IJEItems.Count();

      // Create a new empty Death Record
      DeathRecordSubmission recordSubmission = new DeathRecordSubmission(new DeathRecord());

      // Submit that Death Record
      HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/Bundles", recordSubmission.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, submissionMessage.StatusCode);

      DeathRecordUpdate recordUpdate = new DeathRecordUpdate(recordSubmission.DeathRecord);

      // Submit update message
      HttpResponseMessage updateMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/Bundles", recordUpdate.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, updateMessage.StatusCode);

      Hl7.Fhir.Model.Bundle updatedBundle = null;
      // This code does not have access to the background jobs, the best that can
      // be done is checking to see if the response is correct and if it is still
      // incorrect after the specified delay then assuming that something is wrong
      for(int x = 0; x < 3; ++x) {
        HttpResponseMessage getBundle = await _client.GetAsync("/Bundles");
        updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);
        // Waiting for 2 messages to appear
        if(updatedBundle.Entry.Count > baseBundle.Entry.Count + 1) {
          break;
        } else {
          await Task.Delay(x * 500);
        }
      }

      // Even though the message is a duplicate, it is still ACK'd
      Assert.Equal(baseBundle.Entry.Count + 2, updatedBundle.Entry.Count);

      // Should receive the initial submission message and then an update messaage
      Assert.Equal(ijeItems + 2, _context.IJEItems.Count());
    }
  }
}
