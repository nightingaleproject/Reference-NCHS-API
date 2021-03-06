using messaging.Models;
using Xunit;
using System.Net.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using messaging.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using VRDR;

namespace messaging.tests
{
  [Collection("EndpointIntegrationTests")] // Ensure endpoint tests don't run in parallel
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
    public async System.Threading.Tasks.Task NewSubmissionMessagePostCreatesNewAcknowledgement()
    {
      // Clear any messages in the database for a clean test
      DatabaseHelper.ResetDatabase(_context);

      // Create a new empty Death Record
      DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());

      // Submit that Death Record
      HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", recordSubmission.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

      Hl7.Fhir.Model.Bundle updatedBundle = null;
      // This code does not have access to the background jobs, the best that can
      // be done is checking to see if the response is correct and if it is still
      // incorrect after the specified delay then assuming that something is wrong
      for(int x = 0; x < 5; ++x) {
        HttpResponseMessage oneAck = await _client.GetAsync("/MA/Bundles");
        updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);
        if(updatedBundle.Entry.Count > 0) {
          break;
        } else {
          await System.Threading.Tasks.Task.Delay(x * 500);
        }
      }
      // with the new retrievedAt column, only one message should be returned
      Assert.Single(updatedBundle.Entry);

      // Check to see if the results returned for a jurisdiction other than MA does not return MA entries
      HttpResponseMessage noMessages = await _client.GetAsync("/XX/Bundles");
      var noMessagesBundle = await JsonResponseHelpers.ParseBundleAsync(noMessages);
      Assert.Empty(noMessagesBundle.Entry);
      
      // Check that the retrievedAt column filters out the ACK message if we place another request
      HttpResponseMessage noNewMsgs = await _client.GetAsync("/MA/Bundles");
      Hl7.Fhir.Model.Bundle emptyBundle = await JsonResponseHelpers.ParseBundleAsync(noNewMsgs);
      Assert.Empty(emptyBundle.Entry);
      
      // Extract the message from the bundle and ensure it is an ACK for the appropritae message
      var lastMessageInBundle = updatedBundle.Entry.Last();
      AcknowledgementMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Hl7.Fhir.Model.Bundle)lastMessageInBundle.Resource);
      Assert.Equal(recordSubmission.MessageId, parsedMessage.AckedMessageId);
    }

    [Fact]
    public async System.Threading.Tasks.Task UnparsableMessagesCauseAnError() {
      HttpResponseMessage createBrokenSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", "{}");
      Assert.Equal(HttpStatusCode.BadRequest, createBrokenSubmissionMessage.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task MissingMessageIdCauses400()
    {
        // Create a new empty Death Record and remove MessageId
        DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
        recordSubmission.MessageId = null;

        // Submit that Death Record; should get a 400 back (not 500 as previously observed)
        HttpResponseMessage response = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", recordSubmission.ToJson());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task MissingMessageTypeCauses400()
    {
        // Create a new empty Death Record and remove MessageType
        DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
        recordSubmission.MessageType = null;

        // Submit that Death Record; should get a 400 back (not 500 as previously observed)
        HttpResponseMessage response = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", recordSubmission.ToJson());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task DuplicateSubmissionMessageIsIgnored()
    {
      // Clear any messages in the database for a clean test
      DatabaseHelper.ResetDatabase(_context);

      // Create a new empty Death Record
      DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());

      // Submit that Death Record
      HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", recordSubmission.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

      // Submit Identifical Death Record Again
      HttpResponseMessage duplicateSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", recordSubmission.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, duplicateSubmissionMessage.StatusCode);

      // Make sure the ACKs made it into the queue before querying the endpoint
      Assert.Equal(2, await GetTableCount(_context.OutgoingMessageItems, 2));

      HttpResponseMessage oneAck = await _client.GetAsync("/MA/Bundles");
      Hl7.Fhir.Model.Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);

      // Even though the message is a duplicate, it is still ACK'd
      Assert.Equal(2, updatedBundle.Entry.Count);

      // Since the message is a duplicate, only 1 message per ID is actually parsed.
      Assert.Equal(1, await GetTableCount(_context.IJEItems, 1));
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateMessagesAreSuccessfullyAcknowledged()
    {

      // Clear any messages in the database for a clean test
      DatabaseHelper.ResetDatabase(_context);

      // Get the current time
      DateTime currentTime = DateTime.UtcNow;
      // Create a new empty Death Record
      DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());

      // Submit that Death Record
      HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", recordSubmission.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, submissionMessage.StatusCode);

      DeathRecordUpdateMessage recordUpdate = new DeathRecordUpdateMessage(recordSubmission.DeathRecord);

      // Submit update message
      HttpResponseMessage updateMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", recordUpdate.ToJson());
      Assert.Equal(HttpStatusCode.NoContent, updateMessage.StatusCode);

      // Make sure the ACKs made it into the queue before querying the endpoint
      Assert.Equal(2, await GetTableCount(_context.OutgoingMessageItems, 2));

      Hl7.Fhir.Model.Bundle updatedBundle = null;
      // This code does not have access to the background jobs, the best that can
      // be done is checking to see if the response is correct and if it is still
      // incorrect after the specified delay then assuming that something is wrong
      // use the since parameter to make sure we get both messages
      string since = currentTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
      for(int x = 0; x < 3; ++x) {
        HttpResponseMessage getBundle = await _client.GetAsync("/MA/Bundles?_since=" + since);
        updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);
        // Waiting for 2 messages to appear
        if(updatedBundle.Entry.Count > 1) {
          break;
        } else {
          await System.Threading.Tasks.Task.Delay(x * 500);
        }
      }

      // Even though the message is a duplicate, it is still ACK'd
      Assert.Equal(2, updatedBundle.Entry.Count);

      // Should receive the initial submission message and then an update messaage
      Assert.Equal(2, await GetTableCount(_context.IJEItems, 2));
    }

    // Gets the number of items in the table; retries with cooldown if the expected number is not yet present
    protected async Task<int> GetTableCount<T>(IQueryable<T> table, int expectedCount, int retries = 3, int cooldown = 500) where T : class
    {
        int count = table.Count();
        while (count < expectedCount && --retries > 0)
        {
            await System.Threading.Tasks.Task.Delay(cooldown);
            count = table.Count();
        }
        return count;
    }
    

    [Fact]
    public async System.Threading.Tasks.Task ParseBatchIncomingMessages()
    {
      string batchJson = FixtureStream("fixtures/json/BatchMessages.json").ReadToEnd();
      HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", batchJson);
      Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);

      HttpResponseMessage submissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundles", batchJson);
      Assert.Equal(HttpStatusCode.OK, submissionMessage2.StatusCode);
    }


    [Fact]
    public async System.Threading.Tasks.Task ParseBatchIncomingSingleMessage()
    {
      string batchJson = FixtureStream("fixtures/json/BatchSingleMessage.json").ReadToEnd();
      HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", batchJson);
      Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);

      HttpResponseMessage submissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundles", batchJson);
      Assert.Equal(HttpStatusCode.OK, submissionMessage2.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task ParseBatchIncomingMessagesWithOneError()
    {
      string batchJson = FixtureStream("fixtures/json/BatchWithOneErrorMessage.json").ReadToEnd();
      HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", batchJson);
      Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);

      FhirJsonParser parser = new FhirJsonParser();
      string content = await submissionMessage.Content.ReadAsStringAsync();
      Bundle bundle = parser.Parse<Bundle>(content);
      
      for (int i = 0; i < 2; i++)
      {
        var entry = bundle.Entry[i];
        string status = entry.Response.Status;
        if (i == 0)
        {
          Assert.Equal("400", status);
        }
        if (i == 1)
        {
          Assert.Equal("201", status);
        }
      }

      HttpResponseMessage submissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundles", batchJson);
      Assert.Equal(HttpStatusCode.OK, submissionMessage2.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task ReturnErrorOnInvalidBatch()
    {
      string batchJson = FixtureStream("fixtures/json/BatchInvalidJsonError.json").ReadToEnd();
      HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", batchJson);
      Assert.Equal(HttpStatusCode.BadRequest, submissionMessage.StatusCode);

      HttpResponseMessage submissionMessages2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundles", batchJson);
      Assert.Equal(HttpStatusCode.BadRequest, submissionMessages2.StatusCode);
    }

    private StreamReader FixtureStream(string filePath)
    {
        if (!Path.IsPathRooted(filePath))
        {
            filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
        }
        return File.OpenText(filePath);
    }
  }
}

