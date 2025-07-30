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
using BFDR;


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
        public async System.Threading.Tasks.Task NewDeathSubmissionMessagePostCreatesNewAcknowledgement()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new Death Record
            DeathRecordSubmissionMessage recordSubmission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessage.json"));

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            Hl7.Fhir.Model.Bundle updatedBundle = null;
            // This code does not have access to the background jobs, the best that can
            // be done is checking to see if the response is correct and if it is still
            // incorrect after the specified delay then assuming that something is wrong
            for (int x = 0; x < 10; ++x) {
                HttpResponseMessage oneAck = await _client.GetAsync("/NY/Bundle/VRDR/VRDR_STU3_0");
                updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);
                if (updatedBundle.Entry.Count > 0) {
                    break;
                } else {
                    await System.Threading.Tasks.Task.Delay(x * 500);
                }
            }
            // with the new retrievedAt column, only one message should be returned
            Assert.Single(updatedBundle.Entry);

            // Check to see if the results returned for a jurisdiction other than NY does not return NY entries
            HttpResponseMessage noMessages = await _client.GetAsync("/FL/Bundle/VRDR/VRDR_STU3_0");
            var noMessagesBundle = await JsonResponseHelpers.ParseBundleAsync(noMessages);
            Assert.Empty(noMessagesBundle.Entry);

            // Check that the retrievedAt column filters out the ACK message if we place another request
            HttpResponseMessage noNewMsgs = await _client.GetAsync("/NY/Bundle/VRDR/VRDR_STU3_0");
            Hl7.Fhir.Model.Bundle emptyBundle = await JsonResponseHelpers.ParseBundleAsync(noNewMsgs);
            Assert.Empty(emptyBundle.Entry);

            // Extract the message from the bundle and ensure it is an ACK for the appropritae message
            var lastMessageInBundle = updatedBundle.Entry.Last();
            AcknowledgementMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Hl7.Fhir.Model.Bundle)lastMessageInBundle.Resource);
            Assert.Equal(recordSubmission.MessageId, parsedMessage.AckedMessageId);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetWithParamsDoesNotMarkAsRetrieved()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new Death Record
            DeathRecordSubmissionMessage recordSubmission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessage.json"));

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            // Perform a GET with one parameter
            HttpResponseMessage someParams = await _client.GetAsync("/NY/Bundle/VRDR/VRDR_STU3_0?deathYear=2018");
            Hl7.Fhir.Model.Bundle someParamsBundle = await JsonResponseHelpers.ParseBundleAsync(someParams);
            Assert.Single(someParamsBundle.Entry);

            // Perform a GET with all parameters
            string since = default(DateTime).ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
            HttpResponseMessage allParams = await _client.GetAsync("/NY/Bundle/VRDR/VRDR_STU3_0?deathYear=2018&certificateNumber=" + recordSubmission.CertNo + "&_since=" + since);
            Hl7.Fhir.Model.Bundle allParamsBundle = await JsonResponseHelpers.ParseBundleAsync(allParams);
            Assert.Single(allParamsBundle.Entry);

            // Perform a GET with no parameters; previous GETs should not have marked as retrieved
            HttpResponseMessage noParamsFirst = await _client.GetAsync("/NY/Bundle/VRDR/VRDR_STU3_0");
            Hl7.Fhir.Model.Bundle noParamsFirstBundle = await JsonResponseHelpers.ParseBundleAsync(noParamsFirst);
            Assert.Single(noParamsFirstBundle.Entry);

            // Perform a GET with no parameters; previous GET should have marked as retrieved
            HttpResponseMessage noParamsSecond = await _client.GetAsync("/NY/Bundle/VRDR/VRDR_STU3_0");
            Hl7.Fhir.Model.Bundle noParamsSecondBundle = await JsonResponseHelpers.ParseBundleAsync(noParamsSecond);
            Assert.Empty(noParamsSecondBundle.Entry);
        }

        [Fact]
        public async System.Threading.Tasks.Task NewBirthSubmissionMessagePostCreatesNewAcknowledgement()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // First, create a new Death Record
            DeathRecordSubmissionMessage deathRecordSubmission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessageUT.json"));

            // Set missing required fields
            deathRecordSubmission.MessageSource = "http://example.fhir.org";
            deathRecordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createDeathSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/UT/Bundle", deathRecordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createDeathSubmissionMessage.StatusCode);

            // Create a new Birth Record
            BirthRecordSubmissionMessage recordSubmission = BFDRBaseMessage.Parse<BirthRecordSubmissionMessage>(FixtureStream("fixtures/json/BirthRecordSubmissionMessage.json"));

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            Hl7.Fhir.Model.Bundle updatedBundle = null;
            // This code does not have access to the background jobs, the best that can
            // be done is checking to see if the response is correct and if it is still
            // incorrect after the specified delay then assuming that something is wrong
            for (int x = 0; x < 10; ++x) {
                HttpResponseMessage oneAck = await _client.GetAsync("/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0");
                updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);
                if (updatedBundle.Entry.Count > 0) {
                    break;
                } else {
                    await System.Threading.Tasks.Task.Delay(x * 500);
                }
            }
            // with the new retrievedAt column, only one message should be returned
            Assert.Single(updatedBundle.Entry);

            // Check to see if the results returned for a jurisdiction other than NY does not return NY entries
            HttpResponseMessage noMessages = await _client.GetAsync("/FL/Bundle");
            var noMessagesBundle = await JsonResponseHelpers.ParseBundleAsync(noMessages);
            Assert.Empty(noMessagesBundle.Entry);

            // Check that the retrievedAt column filters out the ACK message if we place another request
            HttpResponseMessage noNewMsgs = await _client.GetAsync("/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0");
            Hl7.Fhir.Model.Bundle emptyBundle = await JsonResponseHelpers.ParseBundleAsync(noNewMsgs);
            Assert.Empty(emptyBundle.Entry);

            // Check that only the death record ack is returned if we place a request to the default endpoint
            HttpResponseMessage newMsgs2 = await _client.GetAsync("/UT/Bundle");
            Hl7.Fhir.Model.Bundle deathRecordBundle = await JsonResponseHelpers.ParseBundleAsync(newMsgs2);
            Assert.Single(deathRecordBundle.Entry);

            // Extract the message from the bundle and ensure it is an ACK for the appropritae message
            var lastMessageInBundle = updatedBundle.Entry.Last();
            BirthRecordAcknowledgementMessage parsedMessage = BFDRBaseMessage.Parse<BirthRecordAcknowledgementMessage>((Hl7.Fhir.Model.Bundle)lastMessageInBundle.Resource);
            Assert.Equal(recordSubmission.MessageId, parsedMessage.AckedMessageId);
        }

        [Fact]
        public async System.Threading.Tasks.Task NewBirthSubmissionMessagePostToBFDRCreatesNewAcknowledgement()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new Birth Record
            BirthRecordSubmissionMessage recordSubmission = BFDRBaseMessage.Parse<BirthRecordSubmissionMessage>(FixtureStream("fixtures/json/BirthRecordSubmissionMessage.json"));

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            Hl7.Fhir.Model.Bundle updatedBundle = null;
            // This code does not have access to the background jobs, the best that can
            // be done is checking to see if the response is correct and if it is still
            // incorrect after the specified delay then assuming that something is wrong
            for (int x = 0; x < 10; ++x) {
                HttpResponseMessage oneAck = await _client.GetAsync("/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0");
                Assert.Equal(HttpStatusCode.OK, oneAck.StatusCode);
                updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);
                if (updatedBundle.Entry.Count > 0) {
                    break;
                } else {
                    await System.Threading.Tasks.Task.Delay(x * 500);
                }
            }
            // with the new retrievedAt column, only one message should be returned
            Assert.Single(updatedBundle.Entry);

            // Check to see if the results returned for a jurisdiction other than UT does not return UT entries
            HttpResponseMessage noMessages = await _client.GetAsync("/FL/Bundle/BFDR-BIRTH/BFDR_STU2_0");
            var noMessagesBundle = await JsonResponseHelpers.ParseBundleAsync(noMessages);
            Assert.Empty(noMessagesBundle.Entry);

            // Check that the retrievedAt column filters out the ACK message if we place another request
            HttpResponseMessage noNewMsgs = await _client.GetAsync("/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0");
            Hl7.Fhir.Model.Bundle emptyBundle = await JsonResponseHelpers.ParseBundleAsync(noNewMsgs);
            Assert.Empty(emptyBundle.Entry);

            // Extract the message from the bundle and ensure it is an ACK for the appropritae message
            var lastMessageInBundle = updatedBundle.Entry.Last();
            BirthRecordAcknowledgementMessage parsedMessage = BFDRBaseMessage.Parse<BirthRecordAcknowledgementMessage>((Hl7.Fhir.Model.Bundle)lastMessageInBundle.Resource);
            Assert.Equal(recordSubmission.MessageId, parsedMessage.AckedMessageId);
        }

        [Fact]
        public async System.Threading.Tasks.Task NewFetalDeathSubmissionMessagePostCreatesNewAcknowledgement()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // First, create a new Death Record
            DeathRecordSubmissionMessage deathRecordSubmission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessageUT.json"));

            // Set missing required fields
            deathRecordSubmission.MessageSource = "http://example.fhir.org";
            deathRecordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createDeathSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/UT/Bundle", deathRecordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createDeathSubmissionMessage.StatusCode);

            // Create a new FetalDeath Record
            FetalDeathRecordSubmissionMessage recordSubmission = BFDRBaseMessage.Parse<FetalDeathRecordSubmissionMessage>(FixtureStream("fixtures/json/FetalDeathRecordSubmissionMessage.json"));

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/UT/Bundle/BFDR-FETALDEATH/BFDR_STU2_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            Hl7.Fhir.Model.Bundle updatedBundle = null;
            // This code does not have access to the background jobs, the best that can
            // be done is checking to see if the response is correct and if it is still
            // incorrect after the specified delay then assuming that something is wrong
            for (int x = 0; x < 10; ++x) {
                HttpResponseMessage oneAck = await _client.GetAsync("/UT/Bundle/BFDR-FETALDEATH/BFDR_STU2_0");
                updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);
                if (updatedBundle.Entry.Count > 0) {
                    break;
                } else {
                    await System.Threading.Tasks.Task.Delay(x * 500);
                }
            }
            // with the new retrievedAt column, only one message should be returned
            Assert.Single(updatedBundle.Entry);

            // Check to see if the results returned for a jurisdiction other than UT does not return UT entries
            HttpResponseMessage noMessages = await _client.GetAsync("/FL/Bundle");
            var noMessagesBundle = await JsonResponseHelpers.ParseBundleAsync(noMessages);
            Assert.Empty(noMessagesBundle.Entry);

            // Check that the retrievedAt column filters out the ACK message if we place another request
            HttpResponseMessage noNewMsgs = await _client.GetAsync("/UT/Bundle/BFDR-FETALDEATH/BFDR_STU2_0");
            Hl7.Fhir.Model.Bundle emptyBundle = await JsonResponseHelpers.ParseBundleAsync(noNewMsgs);
            Assert.Empty(emptyBundle.Entry);

            // Check that only the death record ack is returned if we place a request to the default endpoint
            HttpResponseMessage newMsgs2 = await _client.GetAsync("/UT/Bundle");
            Hl7.Fhir.Model.Bundle deathRecordBundle = await JsonResponseHelpers.ParseBundleAsync(newMsgs2);
            Assert.Single(deathRecordBundle.Entry);

            // Extract the message from the bundle and ensure it is an ACK for the appropritae message
            var lastMessageInBundle = updatedBundle.Entry.Last();
            FetalDeathRecordAcknowledgementMessage parsedMessage = BFDRBaseMessage.Parse<FetalDeathRecordAcknowledgementMessage>((Hl7.Fhir.Model.Bundle)lastMessageInBundle.Resource);
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
                DeathYear = 2020,
                JurisdictionId = "AL"
            };

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/" + recordSubmission.JurisdictionId + "/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            HttpResponseMessage getBundle = await _client.GetAsync("/"  + recordSubmission.JurisdictionId + "/Bundle/VRDR/VRDR_STU3_0?certificateNumber=" + recordSubmission.CertNo + "&deathYear=" + recordSubmission.DeathYear);
            Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);

            Assert.Single(updatedBundle.Entry);
            BaseMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundle.Entry[0].Resource);
            Assert.Equal(recordSubmission.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission.DeathYear, parsedMessage.DeathYear);
        }

        [Fact]
        public async System.Threading.Tasks.Task QueryByBusinessIdsDeathYearPagination()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string jurisdictionId = "ND";
            uint deathYear = 2020;
            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission1 = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 1,
                DeathYear = deathYear,
                JurisdictionId = jurisdictionId
            };

            DeathRecordSubmissionMessage recordSubmission2 = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 2,
                DeathYear = deathYear,
                JurisdictionId = jurisdictionId
            };

            // Submit the Death Records
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/" + jurisdictionId + "/Bundle/VRDR/VRDR_STU3_0", recordSubmission1.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);
            createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/" + jurisdictionId + "/Bundle/VRDR/VRDR_STU3_0", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            HttpResponseMessage getBundle = await _client.GetAsync("/" + jurisdictionId + "/Bundle/VRDR/VRDR_STU3_0?deathYear=" + deathYear + "&_count=1");
            Bundle updatedBundlePage1 = await JsonResponseHelpers.ParseBundleAsync(getBundle);
            Assert.Single(updatedBundlePage1.Entry);
            BaseMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundlePage1.Entry[0].Resource);
            Assert.Equal(recordSubmission1.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission1.DeathYear, parsedMessage.DeathYear);

            getBundle = await _client.GetAsync("/" + jurisdictionId + "/Bundle/VRDR/VRDR_STU3_0?deathYear=" + deathYear + "&_count=1&page=2");
            Bundle updatedBundlePage2 = await JsonResponseHelpers.ParseBundleAsync(getBundle);
            Assert.Single(updatedBundlePage1.Entry);
            parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundlePage2.Entry[0].Resource);
            Assert.Equal(recordSubmission2.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission2.DeathYear, parsedMessage.DeathYear);
        }

        [Fact]
        public async System.Threading.Tasks.Task QueryByBusinessIdsCerficateNumber()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 1,
                DeathYear = 2020,
                JurisdictionId = "NC"
            };

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/" + recordSubmission.JurisdictionId + "/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);
            await System.Threading.Tasks.Task.Delay(1000);

            HttpResponseMessage getBundle = await _client.GetAsync("/" + recordSubmission.JurisdictionId + "/Bundle/VRDR/VRDR_STU3_0?certificateNumber=" + recordSubmission.CertNo);
            Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);

            Assert.Single(updatedBundle.Entry);
            BaseMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundle.Entry[0].Resource);
            Assert.Equal(recordSubmission.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission.DeathYear, parsedMessage.DeathYear);
        }
        
        [Fact]
        public async System.Threading.Tasks.Task QueryByBusinessIdsDeathYear()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 111,
                DeathYear = 2024,
                JurisdictionId = "HI"
            };
            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/" + recordSubmission.JurisdictionId + "/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);
            await System.Threading.Tasks.Task.Delay(1000);

            HttpResponseMessage getBundle = await _client.GetAsync("/" + recordSubmission.JurisdictionId + "/Bundle/VRDR/VRDR_STU3_0?deathYear=" + recordSubmission.DeathYear);
            Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);

            Assert.Single(updatedBundle.Entry);
            BaseMessage parsedMessage = BaseMessage.Parse<AcknowledgementMessage>((Bundle)updatedBundle.Entry[0].Resource);
            Assert.Equal(recordSubmission.CertNo, parsedMessage.CertNo);
            Assert.Equal(recordSubmission.DeathYear, parsedMessage.DeathYear);
        }

        [Fact]
        public async System.Threading.Tasks.Task QueryByDeathYearAndEventYear()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record
            DeathRecordSubmissionMessage drSub = new(new DeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 123,
                DeathYear = 2020,
                JurisdictionId = "ND"
            };

            // Submit that Death Record
            HttpResponseMessage drSubResp = await JsonResponseHelpers.PostJsonAsync(_client, "/" + drSub.JurisdictionId + "/Bundle/VRDR/VRDR_STU3_0", drSub.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, drSubResp.StatusCode);
            await System.Threading.Tasks.Task.Delay(1000);

            // Query Death Record by deathYear
            HttpResponseMessage vrdrByDeathYear = await _client.GetAsync("/" + drSub.JurisdictionId + "/Bundle/VRDR/VRDR_STU3_0?deathYear=" + drSub.DeathYear);
            Bundle vrdrDeathYearBundle = await JsonResponseHelpers.ParseBundleAsync(vrdrByDeathYear);
            Assert.Single(vrdrDeathYearBundle.Entry);

            // Query Death Record by eventYear
            HttpResponseMessage vrdrByEventYear = await _client.GetAsync("/" + drSub.JurisdictionId + "/Bundle/VRDR/VRDR_STU3_0?eventYear=" + drSub.DeathYear);
            Bundle vrdrByEventYearBundle = await JsonResponseHelpers.ParseBundleAsync(vrdrByEventYear);
            Assert.Single(vrdrByEventYearBundle.Entry);

            // Create a new empty Fetal Death Record
            FetalDeathRecordSubmissionMessage fdrSub = new(new FetalDeathRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 123,
                EventYear = 2020,
                JurisdictionId = "ND"
            };

            // Submit that Fetal Death Record
            HttpResponseMessage fdrSubResp = await JsonResponseHelpers.PostJsonAsync(_client, "/" + fdrSub.JurisdictionId + "/Bundle/BFDR-FETALDEATH/BFDR_STU2_0", fdrSub.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, fdrSubResp.StatusCode);
            await System.Threading.Tasks.Task.Delay(1000);

            // Query Fetal Death Record by eventYear
            HttpResponseMessage fdrByEventYear = await _client.GetAsync("/" + fdrSub.JurisdictionId + "/Bundle/BFDR-FETALDEATH/BFDR_STU2_0?eventYear=" + fdrSub.EventYear);
            Bundle fdrByEventYearBundle = await JsonResponseHelpers.ParseBundleAsync(fdrByEventYear);
            Assert.Single(fdrByEventYearBundle.Entry);

            // Create a new empty Birth Record
            BirthRecordSubmissionMessage brSub = new(new BirthRecord())
            {
                // Set missing required fields
                MessageSource = "http://example.fhir.org",
                CertNo = 123,
                EventYear = 2020,
                JurisdictionId = "ND"
            };

            // Submit that Birth Record
            HttpResponseMessage brSubResp = await JsonResponseHelpers.PostJsonAsync(_client, "/" + brSub.JurisdictionId + "/Bundle/BFDR-BIRTH/BFDR_STU2_0", brSub.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, brSubResp.StatusCode);
            await System.Threading.Tasks.Task.Delay(1000);

            // Query Birth Record by eventYear
            HttpResponseMessage brByEventYear = await _client.GetAsync("/" + brSub.JurisdictionId + "/Bundle/BFDR-BIRTH/BFDR_STU2_0?eventYear=" + brSub.EventYear);
            Bundle brByEventYearBundle = await JsonResponseHelpers.ParseBundleAsync(brByEventYear);
            Assert.Single(brByEventYearBundle.Entry);
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
            DeathRecordSubmissionMessage recordSubmission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessage.json"));

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            // Submit Identifical Death Record Again
            HttpResponseMessage duplicateSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, duplicateSubmissionMessage.StatusCode);

            // Make sure the ACKs made it into the queue before querying the endpoint
            Assert.Equal(2, await GetTableCount(_context.OutgoingMessageItems, 2));

            HttpResponseMessage oneAck = await _client.GetAsync("/NY/Bundle/VRDR/VRDR_STU3_0");
            Hl7.Fhir.Model.Bundle updatedBundle = await JsonResponseHelpers.ParseBundleAsync(oneAck);

            // Even though the message is a duplicate, it is still ACK'd
            Assert.Equal(2, updatedBundle.Entry.Count);

            // Since the message is a duplicate, only 1 message per ID is actually parsed.
            Assert.Equal(1, await GetTableCount(_context.IJEItems, 1));
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateDeathMessagesAreSuccessfullyAcknowledged()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Get the current time
            DateTime currentTime = DateTime.UtcNow;
            // Create a new Death Record
            DeathRecordSubmissionMessage recordSubmission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessage.json"));

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;

            // Submit that Death Record
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, submissionMessage.StatusCode);

            DeathRecordUpdateMessage recordUpdate = new DeathRecordUpdateMessage(recordSubmission.DeathRecord);
            
            // Set missing required fields
            recordUpdate.MessageSource = "http://example.fhir.org";
            recordUpdate.CertNo = 1;

            // Submit update message
            HttpResponseMessage updateMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0", recordUpdate.ToJson());
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
                HttpResponseMessage getBundle = await _client.GetAsync("/NY/Bundle/VRDR/VRDR_STU3_0?_since=" + since);
                updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);
                // Waiting for 2 messages to appear
                if (updatedBundle.Entry.Count > 1) {
                    break;
                } else {
                    await System.Threading.Tasks.Task.Delay(x * 500);
                }
            }

            // Both the original and update submission should receive an ACK
            Assert.Equal(2, updatedBundle.Entry.Count);

            // Should receive the initial submission message and then an update messaage
            Assert.Equal(2, await GetTableCount(_context.IJEItems, 2));
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateBirthMessagesAreSuccessfullyAcknowledged()
        {   
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Get the current time
            DateTime currentTime = DateTime.UtcNow;
            // Create a new Birth Record
            BirthRecordSubmissionMessage recordSubmission = BFDRBaseMessage.Parse<BirthRecordSubmissionMessage>(FixtureStream("fixtures/json/BirthRecordSubmissionMessage.json"));

            // Set missing required fields
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            recordSubmission.EventYear = 2024;

            // Submit that Birth Record
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, submissionMessage.StatusCode);

            BirthRecordUpdateMessage recordUpdate = new BirthRecordUpdateMessage(recordSubmission.BirthRecord);
            
            // Set missing required fields
            recordUpdate.MessageSource = "http://example.fhir.org";
            recordUpdate.CertNo = 1;
            recordUpdate.EventYear = 2024;

            // Submit update message
            HttpResponseMessage updateMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0", recordUpdate.ToJson());
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
                HttpResponseMessage getBundle = await _client.GetAsync("/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0?_since=" + since);
                updatedBundle = await JsonResponseHelpers.ParseBundleAsync(getBundle);
                // Waiting for 2 messages to appear
                if (updatedBundle.Entry.Count > 1) {
                    break;
                } else {
                    await System.Threading.Tasks.Task.Delay(x * 500);
                }
            }

            // Both the original and update submission should receive an ACK
            Assert.Equal(2, updatedBundle.Entry.Count);

            // Should receive the initial submission message and then an update messaage
            Assert.Equal(2, await GetTableCount(_context.IJEItems, 2));
        }

        // Gets the number of items in the table; retries with cooldown if the expected number is not yet present
        protected async Task<int> GetTableCount<T>(IQueryable<T> table, int expectedCount, int retries = 5, int cooldown = 500) where T : class
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
        public async System.Threading.Tasks.Task ParseBatchIncomingSingleBirthMessage()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string batchJson = FixtureStream("fixtures/json/BatchSingleBirthMessage.json").ReadToEnd();
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/UT/Bundle", batchJson);
            Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);

            HttpResponseMessage submissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, "/STEVE/UT/Bundle", batchJson);
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
        public async System.Threading.Tasks.Task SpecifyingPageGreaterThanOneRequiresSince()
        {
            HttpResponseMessage getBundles = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?page=3");
            Assert.Equal(HttpStatusCode.BadRequest, getBundles.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task NegativePageInvalid()
        {
            HttpResponseMessage getBundles = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?page=-2");
            Assert.Equal(HttpStatusCode.BadRequest, getBundles.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task NegativeCountPerPageInvalid()
        {
            HttpResponseMessage getBundles = await JsonResponseHelpers.GetAsync(_client, "/MA/Bundle?_count=-50");
            Assert.Equal(HttpStatusCode.BadRequest, getBundles.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task ReturnCorrectNumberOfRecordsWithPagination()
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
          HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0", batchJson);
          Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);

          await System.Threading.Tasks.Task.Delay(1500);
          Assert.Equal(50, await GetTableCount(_context.IncomingMessageItems, 50));

          // wait for acknowledgement generation
          Assert.Equal(50, await GetTableCount(_context.OutgoingMessageItems, 50));

          // The page count should be set to 20, but total should always be set to the total number of records and the
          // "next" link should only appear if there are more results

          // 1st response verify is 20 records
          HttpResponseMessage getBundles1 = await JsonResponseHelpers.GetAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0?_count=20");
          Assert.Equal(HttpStatusCode.OK, getBundles1.StatusCode);

          FhirJsonParser parser = new FhirJsonParser();
          string bundleOfBundles1 = await getBundles1.Content.ReadAsStringAsync();
          Bundle bundle1 = parser.Parse<Bundle>(bundleOfBundles1);
          Assert.Equal(50, bundle1.Total);
          Assert.Equal(20, bundle1.Entry.Count);
          Assert.NotNull(bundle1.NextLink);

          // 2nd response is 20 records
          HttpResponseMessage getBundles2 = await JsonResponseHelpers.GetAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0?_count=20");
          Assert.Equal(HttpStatusCode.OK, getBundles2.StatusCode);

          string bundleOfBundles2 = await getBundles2.Content.ReadAsStringAsync();
          Bundle bundle2 = parser.Parse<Bundle>(bundleOfBundles2);
          Assert.Equal(30, bundle2.Total);
          Assert.Equal(20, bundle2.Entry.Count);
          Assert.NotNull(bundle2.NextLink);

          // 3rd response is 10 records
          HttpResponseMessage getBundles3 = await JsonResponseHelpers.GetAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0?_count=20");
          Assert.Equal(HttpStatusCode.OK, getBundles3.StatusCode);

          string bundleOfBundles3 = await getBundles3.Content.ReadAsStringAsync();
          Bundle bundle3 = parser.Parse<Bundle>(bundleOfBundles3);
          Assert.Equal(10, bundle3.Total);
          Assert.Equal(10, bundle3.Entry.Count);
          Assert.Null(bundle3.NextLink);

          // 4th response is 0 records
          HttpResponseMessage getBundles4 = await JsonResponseHelpers.GetAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0?_count=20");
          Assert.Equal(HttpStatusCode.OK, getBundles4.StatusCode);

          string bundleOfBundles4 = await getBundles4.Content.ReadAsStringAsync();
          Bundle bundle4 = parser.Parse<Bundle>(bundleOfBundles4);
          Assert.Equal(0, bundle4.Total);
          Assert.Empty(bundle4.Entry);
          Assert.Null(bundle4.NextLink);
        }

        [Fact]
        public async System.Threading.Tasks.Task ReturnCorrectNumberOfRecordsWithPaginationAndSince()
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
          HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0", batchJson);
          Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);

          await System.Threading.Tasks.Task.Delay(1500);
          
          Assert.Equal(18, await GetTableCount(_context.IncomingMessageItems, 18));

          // wait for acknowledgement generation
          Assert.Equal(18, await GetTableCount(_context.OutgoingMessageItems, 18));

          // the page count should be set to 5
          // 1st response verify is 5 records
          HttpResponseMessage getBundles = await JsonResponseHelpers.GetAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0?_since=" + startTestFmt + "&_count=5");
          Assert.Equal(HttpStatusCode.OK, getBundles.StatusCode);

          FhirJsonParser parser = new FhirJsonParser();
          string bundleOfBundles = await getBundles.Content.ReadAsStringAsync();
          Bundle bundle = parser.Parse<Bundle>(bundleOfBundles);
          Assert.Equal(5, bundle.Entry.Count);

          // the page count should be set to 5
          // 3rd page should only have 3
          HttpResponseMessage getBundles2 = await JsonResponseHelpers.GetAsync(_client, "/NY/Bundle/VRDR/VRDR_STU3_0?_since=" + startTestFmt + "&_count=5&page=4");
          Assert.Equal(HttpStatusCode.OK, getBundles2.StatusCode);

          string bundleOfBundles2 = await getBundles2.Content.ReadAsStringAsync();
          Bundle bundle2 = parser.Parse<Bundle>(bundleOfBundles2);
          Assert.Equal(3, bundle2.Entry.Count);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostWithInvalidJurisdictionGetsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string badJurisdiction = "AB";
            Assert.False(VR.IJEData.Instance.JurisdictionCodes.ContainsKey(badJurisdiction));

            // Create a new empty Death Record
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/{badJurisdiction}/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);

            // Create a new empty Birth Record
            BirthRecordSubmissionMessage recordSubmission2 = new BirthRecordSubmissionMessage(new BirthRecord());

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/{badJurisdiction}/Bundle", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostWithInvalidFHIRGetsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Submit Death Record with a blank string
            string blankString = FixtureStream("fixtures/json/DeathRecordSubmissionBlankString.json").ReadToEnd();
            HttpResponseMessage blankStringResponse = await JsonResponseHelpers.PostJsonAsync(_client, $"/UT/Bundle", blankString);
            string blankStringBody = await blankStringResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, blankStringResponse.StatusCode);
            Assert.Contains("The property 'text' has an empty string value", blankStringBody);

            // Submit Death Record with a null value
            string nullValue = FixtureStream("fixtures/json/DeathRecordSubmissionNullValue.json").ReadToEnd();
            HttpResponseMessage nullValueResponse = await JsonResponseHelpers.PostJsonAsync(_client, $"/UT/Bundle", nullValue);
            string nullValueBody = await nullValueResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, nullValueResponse.StatusCode);
            Assert.Contains("The property 'text' cannot have just a null value", nullValueBody);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostWithUnescapedStringGetsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Submit Death Record with invalid JSON
            string unescapedString = FixtureStream("fixtures/json/DeathRecordSubmissionUnescapedString.json").ReadToEnd();
            HttpResponseMessage unescapedStringResponse = await JsonResponseHelpers.PostJsonAsync(_client, $"/UT/Bundle", unescapedString);
            string unescapedStringBody = await unescapedStringResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, unescapedStringResponse.StatusCode);
            Assert.Contains("The string should be correctly escaped.", unescapedStringBody);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostWithLeadingZerosGetsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Submit Death Record with a cert number that has leading zeros
            string leadingZeros = FixtureStream("fixtures/json/DeathRecordSubmissionLeadingZeros.json").ReadToEnd();
            HttpResponseMessage leadingZerosResponse = await JsonResponseHelpers.PostJsonAsync(_client, $"/UT/Bundle", leadingZeros);
            string leadingZerosBody = await leadingZerosResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, leadingZerosResponse.StatusCode);
            Assert.Contains("Invalid leading zero before", leadingZerosBody);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostPreservesLocalTime()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Use a record with all datetimes set to using an offset that aligns with PST
            string pstMsg = FixtureStream("fixtures/json/DeathRecordSubmissionMessagePST.json").ReadToEnd();

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/CA/Bundle", pstMsg);
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            // Make sure the record as stored has not had the local times converted from PST
            // Note: running this test on a system actually in PST could create a false positive!
            Assert.Equal(1, await GetTableCount(_context.IncomingMessageItems, 1));
            var messageItem = _context.IncomingMessageItems.First();
            Assert.Contains("\"timestamp\":\"2021-01-20T00:00:00-08:00\"", messageItem.Message);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostCatchMissingSourceEndpoint()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with an empty source
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            Assert.Null(recordSubmission.MessageSource);

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);

            // Create a new empty Birth Record with an empty source
            BirthRecordSubmissionMessage recordSubmission2 = new BirthRecordSubmissionMessage(new BirthRecord());
            Assert.Null(recordSubmission2.MessageSource);

            // Submit that Birth Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostCatchMissingDestinationEndpoint()
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

            // Create a new empty Birth Record with an empty source
            BirthRecordSubmissionMessage recordSubmission2 = new BirthRecordSubmissionMessage(new BirthRecord());
            recordSubmission2.MessageSource = "http://example.fhir.org";
            recordSubmission2.CertNo = 1;
            recordSubmission2.MessageDestination = null;

            // Submit that Birth Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostCatchNCHSIsNotDestinationEndpoint()
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

            // Create a new empty Birth Record WITHOUT nchs as a destination endpoint
            BirthRecordSubmissionMessage recordSubmission2 = new BirthRecordSubmissionMessage(new BirthRecord());
            recordSubmission2.MessageSource = "http://example.fhir.org";
            recordSubmission2.CertNo = 1;
            recordSubmission2.MessageDestination = "http://notnchs.cdc.gov/bfdr_submission";
            // Submit that Birth Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }


        [Fact]
        public async System.Threading.Tasks.Task PostCatchNCHSIsNotDestinationEndpointList()
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

            // Create a new empty Birth Record WITHOUT nchs in endpoint list
            BirthRecordSubmissionMessage recordSubmission2 = new BirthRecordSubmissionMessage(new BirthRecord());
            recordSubmission2.MessageSource = "http://example.fhir.org";
            recordSubmission2.CertNo = 1;
            recordSubmission2.MessageDestination = "http://notnchs.cdc.gov/bfdr_submission,http://steve.org/bfdr_submission";
            // Submit that Birth Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }


        [Fact]
        public async System.Threading.Tasks.Task PostNCHSIsInDestinationEndpointList()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new Death Record WITH nchs in the endpoint list
            DeathRecordSubmissionMessage recordSubmission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessage.json"));

            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            recordSubmission.MessageDestination = "http://notnchs.cdc.gov/vrdr_submission,http://nchs.cdc.gov/vrdr_submission";
            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/NY/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            // Create a new Birth Record WITH nchs in the endpoint list
            BirthRecordSubmissionMessage recordSubmission2 = BFDRBaseMessage.Parse<BirthRecordSubmissionMessage>(FixtureStream("fixtures/json/BirthRecordSubmissionMessage.json"));

            recordSubmission2.MessageSource = "http://example.fhir.org";
            recordSubmission2.CertNo = 1;
            recordSubmission2.MessageDestination = "http://notnchs.cdc.gov/bfdr_submission,http://nchs.cdc.gov/bfdr_submission";
            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/UT/Bundle/BFDR-BIRTH/BFDR_STU2_0", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostNCHSIsInDestinationEndpointListUppercase()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record WITH nchs in the endpoint list
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            recordSubmission.JurisdictionId = "MA";
            recordSubmission.MessageSource = "http://example.fhir.org";
            recordSubmission.CertNo = 1;
            recordSubmission.DeathYear = 2024;
            recordSubmission.MessageDestination = "temp,http://nchs.CDC.gov/VRDR_Submission,temp";
            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage.StatusCode);

            // Create a new empty Birth Record WITH nchs in the endpoint list
            BirthRecordSubmissionMessage recordSubmission2 = new BirthRecordSubmissionMessage(new BirthRecord());
            recordSubmission2.JurisdictionId = "MA";
            recordSubmission2.MessageSource = "http://example.fhir.org";
            recordSubmission2.CertNo = 1;
            recordSubmission2.EventYear = 2024;
            recordSubmission2.MessageDestination = "temp,http://nchs.CDC.gov/BFDR_Submission,temp";
            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle/BFDR-BIRTH/BFDR_STU2_0", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostCatchMissingId()
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

            // Create a new empty Birth Record with an empty message id
            BirthRecordSubmissionMessage recordSubmission2 = new BirthRecordSubmissionMessage(new BirthRecord());
            recordSubmission2.MessageSource = "http://example.fhir.org";
            recordSubmission2.CertNo = 1;
            recordSubmission2.MessageId = null;

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostCatchMissingEventType()
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

            // Create a new empty Birth Record with an empty message type
            BirthRecordSubmissionMessage recordSubmission2 = new BirthRecordSubmissionMessage(new BirthRecord());
            recordSubmission2.MessageSource = "http://example.fhir.org";
            recordSubmission2.CertNo = 1;
            recordSubmission2.MessageType = null;

            // Submit that Birth Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostCatchMissingCertNo()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with a missing cert number
            DeathRecordSubmissionMessage recordSubmission = new DeathRecordSubmissionMessage(new DeathRecord());
            recordSubmission.MessageSource = "http://example.fhir.org";

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);

            // Create a new empty Birth Record with a missing cert number
            BirthRecordSubmissionMessage recordSubmission2 = new BirthRecordSubmissionMessage(new BirthRecord());
            recordSubmission2.MessageSource = "http://example.fhir.org";

            // Submit that Birth Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostWithNonMatchingJurisdictionsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // The jurisdiction ID of the input record submission is 'NY', which should not work with a 'PA' endpoint parameter.
            string jurisdictionParameter = "PA";

            // Create a new Death Record
            DeathRecordSubmissionMessage recordSubmission = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessage.json"));

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/{jurisdictionParameter}/Bundle/VRDR/VRDR_STU3_0", recordSubmission.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);

            // Create a new Birth Record
            BirthRecordSubmissionMessage recordSubmission2 = BFDRBaseMessage.Parse<BirthRecordSubmissionMessage>(FixtureStream("fixtures/json/BirthRecordSubmissionMessage.json"));

            // Submit that Birth Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/{jurisdictionParameter}/Bundle/BFDR-BIRTH/VRDR_STU2_2", recordSubmission2.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostBatchWithNonMatchingJurisdictionsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // The jurisdiction ID of the input record submission is 'MA', which should not work with a 'PA' endpoint parameter.
            string jurisdictionParameter = "PA";

            // Create a new batch message
            string batchJson = FixtureStream("fixtures/json/BatchMessages.json").ReadToEnd();
            HttpResponseMessage submissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/{jurisdictionParameter}/Bundles", batchJson);
            Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);
            Bundle responseBundle = await JsonResponseHelpers.ParseBundleAsync(submissionMessage);
            Assert.Equal("400", responseBundle.Entry[0].Response.Status);
            Assert.Equal("400", responseBundle.Entry[1].Response.Status);

            // Create a new batch message
            string batchJson2 = FixtureStream("fixtures/json/BatchSingleBirthMessage.json").ReadToEnd();
            HttpResponseMessage submissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/{jurisdictionParameter}/Bundles", batchJson2);
            Assert.Equal(HttpStatusCode.OK, submissionMessage.StatusCode);
            Bundle responseBundle2 = await JsonResponseHelpers.ParseBundleAsync(submissionMessage2);
            Assert.Equal("400", responseBundle2.Entry[0].Response.Status);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostCatchInvalidCertNo()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with an invalid cert number
            string invalidCertMsg = FixtureStream("fixtures/json/DeathRecordSubmissionMessageInvalidCertNo.json").ReadToEnd();

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", invalidCertMsg);
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage.StatusCode);

            // Create a new empty Birth Record with an invalid cert number
            string invalidCertMsg2= FixtureStream("fixtures/json/BirthRecordSubmissionMessageInvalidCertNo.json").ReadToEnd();

            // Submit that Birth Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", invalidCertMsg2);
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostCatchInvalidEventYear()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Create a new empty Death Record with an invalid event year number
            string invalidEventYearMsg1 = FixtureStream("fixtures/json/DeathRecordSubmissionMessageInvalidEventYear.json").ReadToEnd();

            // Submit that Death Record
            HttpResponseMessage createSubmissionMessage1 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", invalidEventYearMsg1);
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage1.StatusCode);

            // Create a new empty Birth Record with an invalid event year number
            string invalidEventYearMsg2 = FixtureStream("fixtures/json/BirthRecordSubmissionMessageInvalidEventYear.json").ReadToEnd();

            // Submit that Birth Record
            HttpResponseMessage createSubmissionMessage2 = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundle", invalidEventYearMsg2);
            Assert.Equal(HttpStatusCode.BadRequest, createSubmissionMessage2.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetWithInvalidJurisdictionGetsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            string badJurisdiction = "AB";
            Assert.False(VR.IJEData.Instance.JurisdictionCodes.ContainsKey(badJurisdiction));

            HttpResponseMessage response = await _client.GetAsync($"/{badJurisdiction}/Bundle");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetWithInvalidIGVersionReturnsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            HttpResponseMessage response = await _client.GetAsync($"/NV/Bundle/BFDR-BIRTH/VRDR_STU2_2");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            HttpResponseMessage response2 = await _client.GetAsync($"/NV/Bundle/VRDR/BFDR_STU2_0");
            Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

            HttpResponseMessage response4 = await _client.GetAsync($"/NV/Bundle/typo/VRDR_STU2_2");
            Assert.Equal(HttpStatusCode.NotFound, response4.StatusCode);

            HttpResponseMessage response5 = await _client.GetAsync($"/NV/Bundle/BFDR-FETALDEATH/VRDR_STU2_2");
            Assert.Equal(HttpStatusCode.BadRequest, response5.StatusCode);

            HttpResponseMessage response6 = await _client.GetAsync($"/NV/Bundle/BFDR-FETALDEATH/VRDR_STU3_0");
            Assert.Equal(HttpStatusCode.BadRequest, response6.StatusCode);

            HttpResponseMessage response7 = await _client.GetAsync($"/NV/Bundle/VRDR");
            Assert.Equal(HttpStatusCode.NotFound, response7.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetWithValidIGVersionReturnsOK()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            HttpResponseMessage response = await _client.GetAsync($"/NV/Bundle/BFDR-BIRTH/BFDR_STU2_0");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            HttpResponseMessage response2 = await _client.GetAsync($"/NV/Bundle/VRDR/VRDR_STU2_2");
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            HttpResponseMessage response3 = await _client.GetAsync($"/NV/Bundle/BFDR-FETALDEATH/BFDR_STU2_0");
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);

            HttpResponseMessage response5 = await _client.GetAsync($"/NV/Bundle/VRDR/VRDR_STU3_0");
            Assert.Equal(HttpStatusCode.OK, response5.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task MismatchedUrlPayloadIGVersions()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            DeathRecordSubmissionMessage recordV3_0 = new(new DeathRecord())
            {
                MessageSource = "http://example.fhir.org",
                CertNo = 1,
                DeathYear = 2020,
                JurisdictionId = "AL",
                PayloadVersionId = "VRDR_STU3_0"
            };

            HttpResponseMessage response = await JsonResponseHelpers.PostJsonAsync(_client, "/" + recordV3_0.JurisdictionId + "/Bundle", recordV3_0.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            response = await JsonResponseHelpers.PostJsonAsync(_client, "/" + recordV3_0.JurisdictionId + "/Bundle/VRDR/VRDR_STU2_2", recordV3_0.ToJson());
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostToMultipleIGEndpointsWithBackwardsCompatability ()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // VRDR VRDR_STU2_2 record
            DeathRecordSubmissionMessage recordV2_2 = BaseMessage.Parse<DeathRecordSubmissionMessage>(FixtureStream("fixtures/json/DeathRecordSubmissionMessageV2_2.json"));
            // VRDR VRDR_STU3_0 record
            DeathRecordSubmissionMessage recordV3_0 = new(new DeathRecord())
            {
                MessageSource = "http://example.fhir.org",
                CertNo = 1,
                DeathYear = 2020,
                JurisdictionId = recordV2_2.JurisdictionId
            };

            // POST a VRDR_STU2_2 VRDR record to the default endpoint
            HttpResponseMessage response = await JsonResponseHelpers.PostJsonAsync(_client, $"/{recordV2_2.JurisdictionId}/Bundle", recordV2_2.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Make sure the record does not return for the VRDR_STU3_0 endpoint
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/VRDR/VRDR_STU3_0");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Hl7.Fhir.Model.Bundle bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Empty(bundle.Entry);

            // Check that the record returns for the VRDR_STU2_2 endpoint
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/VRDR/VRDR_STU2_2");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Single(bundle.Entry);

            // Make sure the record does not return now that it's been recieved
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/VRDR/VRDR_STU2_2");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Empty(bundle.Entry);

            // Make sure the record does not return now that it's been recieved, even in the default endpoint
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Empty(bundle.Entry);

            // POST a VRDR VRDR_STU2_2 to the new VRDR_STU2_2 endpoint
            response = await JsonResponseHelpers.PostJsonAsync(_client, $"/{recordV2_2.JurisdictionId}/Bundle/VRDR/VRDR_STU2_2", recordV2_2.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Make sure the record does not return for the VRDR_STU3_0 endpoint
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/VRDR/VRDR_STU3_0");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Empty(bundle.Entry);

            // Check that the record returns for the VRDR_STU2_2 endpoint
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/VRDR/VRDR_STU2_2");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Single(bundle.Entry);

            // Make sure the record does not return now that it's been recieved
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/VRDR/VRDR_STU2_2");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Empty(bundle.Entry);

            // Make sure the record does not return now that it's been recieved, even in the default endpoint
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Empty(bundle.Entry);

            // POST a VRDR_STU2_2 record to the default endpoint
            response = await JsonResponseHelpers.PostJsonAsync(_client, $"/{recordV2_2.JurisdictionId}/Bundle", recordV2_2.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            // POST a VRDR_STU3_0 record to the VRDR_STU3_0 endpoint
            response = await JsonResponseHelpers.PostJsonAsync(_client, $"/{recordV3_0.JurisdictionId}/Bundle/VRDR/VRDR_STU3_0", recordV3_0.ToJson());
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Check that the VRDR_STU3_0 record returns from the VRDR_STU3_0 endpoint
            response = await _client.GetAsync($"/{recordV3_0.JurisdictionId}/Bundle/VRDR/VRDR_STU3_0");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Single(bundle.Entry);

            // Make sure the VRDR_STU3_0 record does not return now that it's been recieved
            response = await _client.GetAsync($"/{recordV3_0.JurisdictionId}/Bundle/VRDR/VRDR_STU3_0");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Empty(bundle.Entry);

            // Check that the VRDR_STU2_2 record returns from the default endpoint
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Single(bundle.Entry);

            // Make sure the record does not return now that it's been recieved, even in the VRDR_STU2_2 endpoint
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/VRDR/VRDR_STU2_2");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Empty(bundle.Entry);

            // Make sure the record does not return now that it's been recieved, even in the default endpoint
            response = await _client.GetAsync($"/{recordV2_2.JurisdictionId}/Bundle/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bundle = await JsonResponseHelpers.ParseBundleAsync(response);
            Assert.Empty(bundle.Entry);
        }

        [Fact]
        public async System.Threading.Tasks.Task PostWithInvalidAckReturnsError()
        {
            // Clear any messages in the database for a clean test
            DatabaseHelper.ResetDatabase(_context);

            // Submit ACK with the wrong jurisdiction
            string ackMA = FixtureStream("fixtures/json/AckMA.json").ReadToEnd();
            HttpResponseMessage ackMAResponse = await JsonResponseHelpers.PostJsonAsync(_client, $"/UT/Bundles", ackMA);
            string ackMABody = await ackMAResponse.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, ackMAResponse.StatusCode);
            Assert.Contains("Message jurisdiction ID MA must match the URL parameter jurisdiction ID UT", ackMABody);

            // Submit ACK with missing cert number
            string ackMissingCertNo = FixtureStream("fixtures/json/AckMissingCertNo.json").ReadToEnd();
            HttpResponseMessage ackMissingCertNoResponse = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundles", ackMissingCertNo);
            string ackMissingCertNoBody = await ackMissingCertNoResponse.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, ackMissingCertNoResponse.StatusCode);
            Assert.Contains("Message certificate number cannot be null", ackMissingCertNoBody);

            // Submit ACK with invalid response code
            string ackInvalidResponseCode = FixtureStream("fixtures/json/AckInvalidResponseCode.json").ReadToEnd();
            HttpResponseMessage ackInvalidResponseCodeResponse = await JsonResponseHelpers.PostJsonAsync(_client, $"/MA/Bundles", ackInvalidResponseCode);
            string ackInvalidResponseCodeBody = await ackInvalidResponseCodeResponse.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, ackInvalidResponseCodeResponse.StatusCode);
            Assert.Contains("'foo' is not a valid value for enumeration 'ResponseType'", ackInvalidResponseCodeBody);
        }

        // TODO create test that sends a VRDR message to the BFDR endpoint and vv, should return an error in both cases

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
