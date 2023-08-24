using messaging.Models;
using Xunit;
using System.Net.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using messaging.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
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
            _context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
        }

        [Fact]
        public async System.Threading.Tasks.Task NewSubmissionMessagePostCreatesNewAcknowledgement()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            Hl7.Fhir.Model.Bundle updatedBundle = null;
            // This code does not have access to the background jobs, the best that can
            // be done is checking to see if the response is correct and if it is still
            // incorrect after the specified delay then assuming that something is wrong
            for (int x = 0; x < 10; ++x) {
                HttpResponseMessage oneAck = await _client.GetAsync("/MA/Bundle");
                updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);
                if (updatedBundle.Entry.Count > 0) {
                    break;
                } else {
                    await System.Threading.Tasks.Task.Delay(x * 500);
                }
            }
            // with the new retrievedAt column, only one message should be returned
            Assert.Single(updatedBundle.Entry);

            // Check to see if the results returned for a jurisdiction other than MA does not return MA entries
            HttpResponseMessage noMessages = await _client.GetAsync("/FL/Bundle");
            var noMessagesBundle = await JsonResponseHelpers.ParseBundleAsync(noMessages);
            Assert.Empty(noMessagesBundle.Entry);

            // Check that the retrievedAt column filters out the ACK message if we place another request
            HttpResponseMessage noNewMsgs = await _client.GetAsync("/MA/Bundle");
            Hl7.Fhir.Model.Bundle emptyBundle = await JsonResponseHelpers.ParseBundleAsync(noNewMsgs);
            Assert.Empty(emptyBundle.Entry);

            // Extract the message from the bundle and ensure it is an ACK for the appropritae message
            var lastMessageInBundle = updatedBundle.Entry.Last();
            AcknowledgementMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Hl7.Fhir.Model.Bundle)lastMessageInBundle.Resource);
            Assert.Equal(recordSubmission.MessageId, parsedMessage.AckedMessageId);
        }

                [Fact]
        public async System.Threading.Tasks.Task QueryByBusinessIdsCerficateNumberAndDeathYear()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 1,
                DeathYear = 2020
            };

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            HttpResponseMessage getBundle = await _client.GetAsync("/MA/Bundle?certificateNumber=" + recordSubmission.CertNo + "&deathYear=" + recordSubmission.DeathYear);
            Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);

            Assert.Single(updatedBundle.Entry);
            BaseMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundle.Entry[0].Resource);
            Assert.Equal(recordSubmission.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission.DeathYear, parsedMessage.DeathYear);
        }

        public async System.Threading.Tasks.Task QueryByBusinessIdsDeathYearPagination()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission1 = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 1,
                DeathYear = 2020
            };

            DeathRecordSubmissionMessage recordSubmission2 = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 2,
                DeathYear = 2020
            };

            // Submit the Death Records
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordSubmission1.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);
            createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            HttpResponseMessage getBundle = await _client.GetAsync("/MA/Bundle?deathYear=2020&_count=1");
            Bundle updatedBundlePage1 = await JsonResponseHelpers.ParseBundleAsync(getBundle);
            Assert.Single(updatedBundlePage1.Entry);
            BaseMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundlePage1.Entry[0].Resource);
            Assert.Equal(recordSubmission1.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission1.DeathYear, parsedMessage.DeathYear);

            getBundle = await _client.GetAsync("/MA/Bundle?deathYear=2020&_count=1&page=2");
            Bundle updatedBundlePage2 = await JsonResponseHelpers.ParseBundleAsync(getBundle);
            Assert.Single(updatedBundlePage1.Entry);
            parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundlePage2.Entry[0].Resource);
            Assert.Equal(recordSubmission2.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission2.DeathYear, parsedMessage.DeathYear);
        }

        public async System.Threading.Tasks.Task QueryByBusinessIdCerficateNumber()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 1,
                DeathYear = 2020
            };

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            HttpResponseMessage getBundle = await _client.GetAsync("/MA/Bundle?certificateNumber=" + recordSubmission.CertNo.ToString().PadLeft(6, '0'));
            Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);

            Assert.Single(updatedBundle.Entry);
            BaseMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundle.Entry[0].Resource);
            Assert.Equal(recordSubmission.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission.DeathYear, parsedMessage.DeathYear);
        }

        public async System.Threading.Tasks.Task QueryByBusinessIdsDeathYear()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 1,
                DeathYear = 2020
            };

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            HttpResponseMessage getBundle = await _client.GetAsync("/MA/Bundle?&deathYear=" + recordSubmission.DeathYear);
            Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);

            Assert.Single(updatedBundle.Entry);
            BaseMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundle.Entry[0].Resource);
            Assert.Equal(recordSubmission.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission.DeathYear, parsedMessage.DeathYear);
        }

        [Fact]
        public async System.Threading.Tasks.Task UnparsableMessagesCauseAnError() {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            HttpResponseMessage createBrokenSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", "{}");
            Assert.Equal(HttpStatusCode.BadRequest, createBrokenSubmissionMessage.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task DuplicateSubmissionMessageIsIgnored()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            // Submit Identifical Death Record Again
            HttpResponseMessage duplicateSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, duplicateSubmissionMessage.StatusCode);

            // Make sure the ACKs made it into the queue before querying the endpoint
            Assert.Equal(2, await GetTableCount(_context.OutgoingMessageItems, 2));

            HttpResponseMessage oneAck = await _client.GetAsync("/MA/Bundle");
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

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, submissionMessage.StatusCode);

            DeathRecordUpdateMessage recordUpdate = new DeathRecordUpdateMessage(recordSubmission.DeathRecord);
            
            // Set missing required fields
            recordUpdate.MessageSource = "http://example.fhir.org";
            recordUpdate.CertNo = 1;

            // Submit update message
            HttpResponseMessage updateMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", recordUpdate.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, updateMessage.StatusCode);

            // Make sure the ACKs made it into the queue before querying the endpoint
            Assert.Equal(2, await GetTableCount(_context.OutgoingMessageItems, 2));

            Hl7.Fhir.Model.Bundle updatedBundle = null;
            // This code does not have access to the background jobs, the best that can
            // be done is checking to see if the response is correct and if it is still
            // incorrect after the specified delay then assuming that something is wrong
            // use the since parameter to make sure we get both messages
            string since = currentTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
            for (int x = 0; x < 3; ++x) {
                HttpResponseMessage getBundle = await _client.GetAsync("/MA/Bundle?_since=" + since);
                updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);
                // Waiting for 2 messages to appear
                if (updatedBundle.Entry.Count > 1) {
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
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string batchJson = FixtureStream("fixtures/json/BatchMessages.json").ReadToEnd();
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", batchJson);
            Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);

            HttpResponseMessage submissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundle", batchJson);
            Assert.Equal(HttpStatusCode.OK, submissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task ParseBatchIncomingMessagesBackwardsCompatibility()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string batchJson = FixtureStream("fixtures/json/BatchMessages.json").ReadToEnd();
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundles", batchJson);
            Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);

            HttpResponseMessage submissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundles", batchJson);
            Assert.Equal(HttpStatusCode.OK, submissionMessage2.StatusCode);
        }


        [Fact]
        public async System.Threading.Tasks.Task ParseBatchIncomingSingleMessage()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string batchJson = FixtureStream("fixtures/json/BatchSingleMessage.json").ReadToEnd();
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", batchJson);
            Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);

            HttpResponseMessage submissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundle", batchJson);
            Assert.Equal(HttpStatusCode.OK, submissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task ParseBatchIncomingMessagesWithOneError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string batchJson = FixtureStream("fixtures/json/BatchWithOneErrorMessage.json").ReadToEnd();
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", batchJson);
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

            HttpResponseMessage submissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundle", batchJson);
            Assert.Equal(HttpStatusCode.OK, submissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task ReturnErrorOnInvalidBatch()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string batchJson = FixtureStream("fixtures/json/BatchInvalidJsonError.json").ReadToEnd();
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", batchJson);
            Assert.Equal(HttpStatusCode.BadRequest, submissionMessage.StatusCode);

            HttpResponseMessage submissionMessages2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundle", batchJson);
            Assert.Equal(HttpStatusCode.BadRequest, submissionMessages2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task ReturnErrorOnSubmittedExtractionError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string extErrJson = FixtureStream("fixtures/json/ExtractionErrorMessage.json").ReadToEnd();
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", extErrJson);
            Assert.Equal(HttpStatusCode.BadRequest, submissionMessage.StatusCode);

            HttpResponseMessage submissionMessages2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundle", extErrJson);
            Assert.Equal(HttpStatusCode.BadRequest, submissionMessages2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task ReturnErrorOnSubmittedCodingMessage()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string codeMsg = FixtureStream("fixtures/json/CauseOfDeathCodingMsg.json").ReadToEnd();
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", codeMsg);
            Assert.Equal(HttpStatusCode.BadRequest, submissionMessage.StatusCode);

            HttpResponseMessage submissionMessages2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/MA/Bundle", codeMsg);
            Assert.Equal(HttpStatusCode.BadRequest, submissionMessages2.StatusCode);
        }
        
        [Fact]
        public async void SpecifyingPageGreaterThanOneRequiresSince()
        {
            HttpResponseMessage getBundles = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?page=3");
            Assert.Equal(HttpStatusCode.BadRequest, getBundles.StatusCode);
        }

        [Fact]
        public async void NegativePageInvalid()
        {
            HttpResponseMessage getBundles = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?page=-2");
            Assert.Equal(HttpStatusCode.BadRequest, getBundles.StatusCode);
        }

        [Fact]
        public async void NegativeCountPerPageInvalid()
        {
            HttpResponseMessage getBundles = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?_count=-50");
            Assert.Equal(HttpStatusCode.BadRequest, getBundles.StatusCode);
        }

        [Fact]
        public async void ReturnCorrectNumberOfRecordsWithPagination()
        {
        
          // Clear any messages in the database for a clean test
          DatabaseHelper.ResetDatabase(_context);

          // the test should insert 50 records
          Bundle batchMsg = new Bundle();
          batchMsg.Type = Bundle.BundleType.Batch;
          DeathRecordSubmissionMessage submission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessage.json"));
          for (int i = 0; i < 50; i++)
          {
              submission.CertNo = (uint?)i;
              Bundle.EntryComponent entry = new Bundle.EntryComponent();
              entry.Resource = (Resource)submission;
              batchMsg.Entry.Add(entry);
          }

          string batchJson = batchMsg.ToJson();
          HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", batchJson);
          Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);
          Assert.Equal(50, await GetTableCount(_context.IncomingMessageItems, 50));

          // wait for acknowledgement generation
          Assert.Equal(50, await GetTableCount(_context.OutgoingMessageItems, 50));

          // The page count should be set to 20, but total should always be set to the total number of records and the
          // "next" link should only appear if there are more results

          // 1st response verify is 20 records
          HttpResponseMessage getBundles1 = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?_count=20");
          Assert.Equal(HttpStatusCode.OK, getBundles1.StatusCode);

          FhirJsonParser parser = new FhirJsonParser();
          string bundleOfBundles1 = await getBundles1.Content.ReadAsStringAsync();
          Bundle bundle1 = parser.Parse<Bundle>(bundleOfBundles1);
          Assert.Equal(50, bundle1.Total);
          Assert.Equal(20, bundle1.Entry.Count);
          Assert.NotNull(bundle1.NextLink);

          // 2nd response is 20 records
          HttpResponseMessage getBundles2 = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?_count=20");
          Assert.Equal(HttpStatusCode.OK, getBundles2.StatusCode);

          string bundleOfBundles2 = await getBundles2.Content.ReadAsStringAsync();
          Bundle bundle2 = parser.Parse<Bundle>(bundleOfBundles2);
          Assert.Equal(30, bundle2.Total);
          Assert.Equal(20, bundle2.Entry.Count);
          Assert.NotNull(bundle2.NextLink);

          // 3rd response is 10 records
          HttpResponseMessage getBundles3 = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?_count=20");
          Assert.Equal(HttpStatusCode.OK, getBundles3.StatusCode);

          string bundleOfBundles3 = await getBundles3.Content.ReadAsStringAsync();
          Bundle bundle3 = parser.Parse<Bundle>(bundleOfBundles3);
          Assert.Equal(10, bundle3.Total);
          Assert.Equal(10, bundle3.Entry.Count);
          Assert.Null(bundle3.NextLink);

          // 4th response is 0 records
          HttpResponseMessage getBundles4 = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?_count=20");
          Assert.Equal(HttpStatusCode.OK, getBundles4.StatusCode);

          string bundleOfBundles4 = await getBundles4.Content.ReadAsStringAsync();
          Bundle bundle4 = parser.Parse<Bundle>(bundleOfBundles4);
          Assert.Equal(0, bundle4.Total);
          Assert.Empty(bundle4.Entry);
          Assert.Null(bundle4.NextLink);
        }

        [Fact]
        public async void ReturnCorrectNumberOfRecordsWithPaginationAndSince()
        {
          // Clear any messages in the database for a clean test
          DatabaseHelper.ResetDatabase(_context);

          DateTime startTest = DateTime.UtcNow;
          var startTestFmt = startTest.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
          // the test should insert 18 records
          Bundle batchMsg = new Bundle();
          batchMsg.Type = Bundle.BundleType.Batch;
          DeathRecordSubmissionMessage submission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessage.json"));
          for (int i = 0; i < 18; i++)
          {
              submission.CertNo = (uint?)i;
              Bundle.EntryComponent entry = new Bundle.EntryComponent();
              entry.Resource = (Resource)submission;
              batchMsg.Entry.Add(entry);
          }

          string batchJson = batchMsg.ToJson();
          HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/MA/Bundle", batchJson);
          Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);
          Assert.Equal(18, await GetTableCount(_context.IncomingMessageItems, 18));

          // wait for acknowledgement generation
          Assert.Equal(18, await GetTableCount(_context.OutgoingMessageItems, 18));

          // the page count should be set to 5
          // 1st response verify is 5 records
          HttpResponseMessage getBundles = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?_since=" + startTestFmt + "&_count=5");
          Assert.Equal(HttpStatusCode.OK, getBundles.StatusCode);

          FhirJsonParser parser = new FhirJsonParser();
          string bundleOfBundles = await getBundles.Content.ReadAsStringAsync();
          Bundle bundle = parser.Parse<Bundle>(bundleOfBundles);
          Assert.Equal(5, bundle.Entry.Count);

          // the page count should be set to 5
          // 3rd page should only have 3
          HttpResponseMessage getBundles2 = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?_since=" + startTestFmt + "&_count=5&page=4");
          Assert.Equal(HttpStatusCode.OK, getBundles2.StatusCode);

          string bundleOfBundles2 = await getBundles2.Content.ReadAsStringAsync();
          Bundle bundle2 = parser.Parse<Bundle>(bundleOfBundles2);
          Assert.Equal(3, bundle2.Entry.Count);
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
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/{badJurisdiction}/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }

        [Fact]
        public async void PostCatchMissingSourceEndpoint()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with an empty source
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            Assert.Null(recordSubmission.MessageSource);

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }

        [Fact]
        public async void PostCatchMissingDestinationEndpoint()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with an empty source
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            recordSubmission.MessageDestination = null;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }


        [Fact]
        public async void PostCatchNCHSIsNotDestinationEndpoint()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record WITHOUT nchs as a destination endpoint
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            recordSubmission.MessageDestination = "http://notnchs.cdc.gov/vrdr_submission";
            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }


        [Fact]
        public async void PostCatchNCHSIsNotDestinationEndpointList()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record WITHOUT nchs in endpoint list
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            recordSubmission.MessageDestination = "http://notnchs.cdc.gov/vrdr_submission,http://steve.org/vrdr_submission";
            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }


        [Fact]
        public async void PostNCHSIsInDestinationEndpointList()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record WITH nchs in the endpoint list
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            recordSubmission.MessageDestination = "http://notnchs.cdc.gov/vrdr_submission,http://nchs.cdc.gov/vrdr_submission";
            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);
        }

        [Fact]
        public async void PostCatchMissingId()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with an empty message id
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            recordSubmission.MessageId = null;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }

        [Fact]
        public async void PostCatchMissingEventType()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with an empty message type
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            recordSubmission.MessageType = null;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }

        [Fact]
        public async void PostCatchMissingCertNo()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with a missing cert number
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            recordSubmission.MessageSource = "http://example.fhir.org";

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }

        [Fact]
        public async void PostCatchInvalidCertNo()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with an invalid cert number
            string invalidCertMsg = FixtureStream("fixtures/json/DeathRecordSubmissionMessageInvalidCertNo.json").ReadToEnd();

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", invalidCertMsg);
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);
        }

        [Fact]
        public async void GetWithInvalidJurisdictionGetsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string badJurisdiction = "AB";
            Assert.False(VRDR.MortalityData.Instance.JurisdictionCodes.ContainsKey(badJurisdiction));

            HttpResponseMessage response = await _client.GetAsync($"/{badJurisdiction}/Bundle");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        public static StreamReader FixtureStream(string filePath)
        {
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
            }
            return File.OpenText(filePath);
        }
    }
}

