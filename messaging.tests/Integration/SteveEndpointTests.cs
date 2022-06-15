using Xunit;
using System.Net.Http;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using messaging.tests.Helpers;
using VRDR;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using messaging.Models;

namespace messaging.tests
{
    [Collection("EndpointIntegrationTests")] // Ensure endpoint tests don't run in parallel
    public class SteveEndpointTests : IClassFixture<CustomWebApplicationFactory<messaging.Startup>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<messaging.Startup> _factory;

        private readonly ApplicationDbContext _context;

        private readonly string STEVE_ENDPOINT = "/STEVE/MA/Bundles";
        private readonly string MA_ENDPOINT = "/MA/Bundles";

        public SteveEndpointTests(CustomWebApplicationFactory<messaging.Startup> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            IServiceScope serviceScope = factory.Services.GetService<IServiceScopeFactory>().CreateScope();
            _context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
        }

        [Fact]
        public async Task NewSubmissionMessagePostCreatesNewAcknowledgement()
        {
            // Clear any waiting messages in the queue
            await _client.GetAsync(STEVE_ENDPOINT);

            // Create and submit a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            Hl7.Fhir.Model.Bundle updatedBundle = await GetQueuedMessages(STEVE_ENDPOINT);
            Assert.Single(updatedBundle.Entry);

            // Check to see if the results returned for a jurisdiction other than MA does not return MA entries
            HttpResponseMessage noMessages = await _client.GetAsync("STEVE/XX/Bundles");
            var noMessagesBundle = await JsonResponseHelpers.ParseBundleAsync(noMessages);
            Assert.Empty(noMessagesBundle.Entry);

            // Extract the message from the bundle and ensure it is an ACK for the appropritae message
            var lastMessageInBundle = updatedBundle.Entry.Single();
            AcknowledgementMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Hl7.Fhir.Model.Bundle)lastMessageInBundle.Resource);
            Assert.Equal(recordSubmission.MessageId, parsedMessage.AckedMessageId);
        }

        [Fact]
        public async Task UnparsableMessagesCauseAnError()
        {
            HttpResponseMessage createBrokenSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, "{}");
            Assert.Equal(HttpStatusCode.BadRequest, createBrokenSubmissionMessage.StatusCode);
        }

        [Fact]
        public async Task DuplicateSubmissionMessageIsIgnored()
        {
            // Clear any waiting messages in the queue
            await _client.GetAsync(STEVE_ENDPOINT);

            // Get the current size of the number of IJEItems in the database
            var ijeItems = _context.IJEItems.Count();

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            // Submit Identifical Death Record Again
            HttpResponseMessage duplicateSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, duplicateSubmissionMessage.StatusCode);

            // Since the background task should ignore duplicate messages if working correctly,
            // provide ample time for it to finish.
            await Task.Delay(4000);

            HttpResponseMessage oneAck = await _client.GetAsync(STEVE_ENDPOINT);
            Hl7.Fhir.Model.Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);

            // Even though the message is a duplicate, it is still ACK'd
            Assert.Equal(2, updatedBundle.Entry.Count);

            // Since the message is a duplicate, only 1 message per ID is actually parsed.
            Assert.Equal(ijeItems + 1, _context.IJEItems.Count());
        }

        [Fact]
        public async Task UpdateMessagesAreSuccessfullyAcknowledged()
        {
            // Clear any waiting messages in the queue
            await _client.GetAsync(STEVE_ENDPOINT);

            // Get the current size of the number of IJEItems in the database
            var ijeItems = _context.IJEItems.Count();

            // Create and submit a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, submissionMessage.StatusCode);

            DeathRecordUpdateMessage recordUpdate = new DeathRecordUpdateMessage(recordSubmission.DeathRecord);

            // Submit update message
            HttpResponseMessage updateMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordUpdate.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, updateMessage.StatusCode);

            Hl7.Fhir.Model.Bundle updatedBundle = await GetQueuedMessages(STEVE_ENDPOINT);

            // Even though the message is a duplicate, it is still ACK'd
            Assert.Equal(2, updatedBundle.Entry.Count);

            // Should receive the initial submission message and then an update messaage
            Assert.Equal(ijeItems + 2, _context.IJEItems.Count());
        }

        [Fact]
        public async Task SteveAndJurisdictionBothRetrieveSteveSubmission()
        {
            // Clear any waiting messages in the queue for both STEVE and the jurisdiction
            await _client.GetAsync(STEVE_ENDPOINT);
            await _client.GetAsync(MA_ENDPOINT);

            // Create and submit a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, submissionMessage.StatusCode);

            // Get the STEVE response
            Hl7.Fhir.Model.Bundle response = await GetQueuedMessages(STEVE_ENDPOINT);
            Assert.Single(response.Entry);

            // Get the Jurisdiction response (don't need the retries because it is known to be in the queue)
            HttpResponseMessage jurisdictionResponse = await _client.GetAsync(MA_ENDPOINT);
            response = await JsonResponseHelpers.ParseBundleAsync(jurisdictionResponse);
            Assert.Single(response.Entry);
        }

        private async Task<Hl7.Fhir.Model.Bundle> GetQueuedMessages(string endpoint, int retries = 3, int cooldown = 500)
        {
            Hl7.Fhir.Model.Bundle queued = null;
            // Can't be sure how long it will take; retry a couple times with cooldown in between before assuming failure
            for (int x = 0; x < retries; ++x)
            {
                HttpResponseMessage oneAck = await _client.GetAsync(STEVE_ENDPOINT);
                queued = await JsonResponseHelpers.ParseBundleAsync(oneAck);
                if (queued.Entry.Count > 0)
                {
                    return queued;
                }
                await Task.Delay(x * cooldown);
            }
            return queued;
        }
    }
}
