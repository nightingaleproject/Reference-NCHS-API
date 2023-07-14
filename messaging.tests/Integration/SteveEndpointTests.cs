using messaging.Models;
using messaging.tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using VRDR;
using Xunit;

namespace messaging.tests
{
    [Collection("EndpointIntegrationTests")] // Ensure endpoint tests don't run in parallel
    public class SteveEndpointTests : IClassFixture<CustomWebApplicationFactory<messaging.Startup>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<messaging.Startup> _factory;

        private readonly ApplicationDbContext _context;

        private readonly string STEVE_ENDPOINT = "/STEVE/MA/Bundle";
        private readonly string MA_ENDPOINT = "/MA/Bundle";

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
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create and submit a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            
            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            // Make sure the ACKs made it into the queue before querying the endpoint
            Assert.Equal(1, await GetTableCount(_context.OutgoingMessageItems, 1));

            Hl7.Fhir.Model.Bundle updatedBundle = await GetQueuedMessages(STEVE_ENDPOINT);
            Assert.Single(updatedBundle.Entry);

            // Check to see if the results returned for a jurisdiction other than MA does not return MA entries
            HttpResponseMessage noMessages = await _client.GetAsync("STEVE/FL/Bundle");
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
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            
            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            // Submit Identifical Death Record Again
            HttpResponseMessage duplicateSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, duplicateSubmissionMessage.StatusCode);

            // Make sure the ACKs made it into the queue before querying the endpoint
            Assert.Equal(2, await GetTableCount(_context.OutgoingMessageItems, 2));

            HttpResponseMessage oneAck = await _client.GetAsync(STEVE_ENDPOINT);
            Hl7.Fhir.Model.Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);

            // Even though the message is a duplicate, it is still ACK'd
            Assert.Equal(2, updatedBundle.Entry.Count);

            // Since the message is a duplicate, only 1 message per ID is actually parsed.
            Assert.Equal(1, await GetTableCount(_context.IJEItems, 1));
        }

        [Fact]
        public async Task UpdateMessagesAreSuccessfullyAcknowledged()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create and submit a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            
            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, submissionMessage.StatusCode);

            DeathRecordUpdateMessage recordUpdate = new DeathRecordUpdateMessage(recordSubmission.DeathRecord);
            
            // Set missing required fields
            recordUpdate.MessageSource = "http://example.fhir.org";
            recordUpdate.CertNo = 1;

            // Submit update message
            HttpResponseMessage updateMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordUpdate.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, updateMessage.StatusCode);

            // Make sure the ACKs made it into the queue before querying the endpoint
            Assert.Equal(2, await GetTableCount(_context.OutgoingMessageItems, 2));

            Hl7.Fhir.Model.Bundle updatedBundle = await GetQueuedMessages(STEVE_ENDPOINT);

            // Even though the message is a duplicate, it is still ACK'd
            Assert.Equal(2, updatedBundle.Entry.Count);

            // Should receive the initial submission message and then an update messaage
            Assert.Equal(2, await GetTableCount(_context.IJEItems, 2));
        }

        [Fact]
        public async Task SteveAndJurisdictionBothRetrieveSteveSubmission()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create and submit a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, STEVE_ENDPOINT, recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, submissionMessage.StatusCode);

            // Make sure the ACKs made it into the queue before querying the endpoint
            Assert.Equal(1, await GetTableCount(_context.OutgoingMessageItems, 1));

            // Get the STEVE response
            Hl7.Fhir.Model.Bundle response = await GetQueuedMessages(STEVE_ENDPOINT);
            Assert.Single(response.Entry);

            // Get the Jurisdiction response (don't need the retries because it is known to be in the queue)
            HttpResponseMessage jurisdictionResponse = await _client.GetAsync(MA_ENDPOINT);
            response = await JsonResponseHelpers.ParseBundleAsync(jurisdictionResponse);
            Assert.Single(response.Entry);
        }

        [Fact]
        public async void PostWithInvalidJurisdictionGetsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string badJurisdiction = "AB";
            Assert.False(VRDR.MortalityData.Instance.JurisdictionCodes.ContainsKey(badJurisdiction));

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/STEVE/{badJurisdiction}/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }

        [Fact]
        public async void GetWithInvalidJurisdictionGetsError()
        {
            string badJurisdiction = "AB";
            Assert.False(VRDR.MortalityData.Instance.JurisdictionCodes.ContainsKey(badJurisdiction));

            HttpResponseMessage response = await _client.GetAsync($"/STEVE/{badJurisdiction}/Bundle");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

        // Gets the number of items in the table; retries with cooldown if the expected number is not yet present
        protected async Task<int> GetTableCount<T>(IQueryable<T> table, int expectedCount, int retries = 3, int cooldown = 500) where T : class
        {
            int count = table.Count();
            while (count < expectedCount && --retries > 0)
            {
                await Task.Delay(cooldown);
                count = table.Count();
            }
            return count;
        }
    }
}

