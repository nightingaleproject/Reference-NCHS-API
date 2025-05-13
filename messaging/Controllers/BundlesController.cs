using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using messaging.Models;
using messaging.Services;
using Hl7.Fhir.Model;
using VRDR;
using BFDR;
using VR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hl7.Fhir.Utility;
using System.Threading;

namespace messaging.Controllers
{
    [Route("{jurisdictionId:length(2)}/Bundle")]
    [Route("{jurisdictionId:length(2)}/Bundle/{vitalType:regex(^(VRDR|BFDR-BIRTH|BFDR-FETALDEATH)$)}/{igVersion}")]
    [Route("{jurisdictionId:length(2)}/Bundles")] // Historical endpoint for backwards compatibility
    [Produces("application/json")]
    [ApiController]
    public class BundlesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider Services;
        protected readonly AppSettings _settings;
        protected readonly ILogger<BundlesController> _logger;

        public BundlesController(ILogger<BundlesController> logger, ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings)
        {
            _context = context;
            Services = services;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves outgoing messages for the jurisdiction
        /// If the optional Certificate Number and Death year parameters are provided, retrieves all messages in history that match those given business ids.
        /// </summary>
        /// <returns>A Bundle of FHIR messages</returns>
        /// <response code="200">Content retrieved successfully</response>
        /// <response code="400">Bad Request</response>
        /// <response code="500">Internal Error, token may have expired</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Bundle>> GetOutgoingMessageItems(string jurisdictionId, string vitalType, string igVersion, int _count, string certificateNumber, string deathYear, DateTime _since = default(DateTime), int page = 1)
        {
            if (_count == 0)
            {
                _count = _settings.PageCount;
            }
            if (!VR.IJEData.Instance.JurisdictionCodes.ContainsKey(jurisdictionId))
            {
                // Don't log the jurisdictionId value itself, since it is (known-invalid) user input
                _logger.LogError("Rejecting request with invalid jurisdiction ID.");
                return BadRequest("Invalid jurisdiction ID");
            }
            if (_count < 0)
            {
                _logger.LogError("Rejecting request with invalid count parameter.");
                return BadRequest("_count must not be negative");
            }
            if (page < 1)
            {
                _logger.LogError("Rejecting request with invalid page number.");
                return BadRequest("page must not be negative");
            }
            if (!ValidateVitalTypeIGVersion(ref vitalType, ref igVersion, out BadRequestObjectResult br))
            {
                return br;
            }

            string recordType = "";
            switch (vitalType.ToUpper())
            {
                case "VRDR":
                    recordType = "MOR";
                    break;
                case "BFDR-BIRTH":
                    recordType = "NAT";
                    break;
                case "BFDR-FETALDEATH":
                    recordType = "FET";
                    break;
            }

            bool additionalParamsProvided = !(_since == default(DateTime) && certificateNumber == null && deathYear == null);
            // Retrieving unread messages changes the result set (as they get marked read), so we don't REALLY support paging
            if (!additionalParamsProvided && page > 1)
            {
                _logger.LogError("Rejecting request with a page number but no _since parameter.");
                return BadRequest("Pagination does not support specifying a page without either a _since, certificateNumber, or deathYear parameter");
            }
            _logger.LogDebug($"Provided params: {certificateNumber}, {deathYear}, {_since}");
            
            RouteValueDictionary searchParamValues = new()
            {
                { "jurisdictionId", jurisdictionId },
                { "_count", _count }
            };
            if (certificateNumber != null) {
                // Pad left with leading zeros if not a 6-digit certificate number.
                certificateNumber = certificateNumber.PadLeft(6, '0');
                searchParamValues.Add("certificateNumber", certificateNumber);
            }
            if (deathYear != null) {
                searchParamValues.Add("deathYear", deathYear);
            }

            // Query for outgoing messages of the requested type by jurisdiction ID. Filter by IG version. Optionally filter by certificate number and death year if those parameters are provided.
            IQueryable<OutgoingMessageItem> outgoingMessagesQuery = _context.OutgoingMessageItems.Where(message => message.JurisdictionId == jurisdictionId
                    && (String.IsNullOrEmpty(recordType) || message.EventType.Equals(recordType))
                    && (certificateNumber == null || message.CertificateNumber.Equals(certificateNumber))
                    && (deathYear == null || message.EventYear == int.Parse(deathYear)));

            try
            {
                // Further scope the search to either unretrieved messages (or all since a specific time)
                // TODO only allow the since param in development
                // if _since is the default value, then apply the retrieved at logic unless certificate number or death year are provided
                if (!additionalParamsProvided)
                {
                    outgoingMessagesQuery = ExcludeRetrieved(outgoingMessagesQuery);
                }
                if (_since != default(DateTime))
                {
                    outgoingMessagesQuery = outgoingMessagesQuery.Where(message => message.CreatedDate >= _since);
                }

                int totalMessageCount = outgoingMessagesQuery.Count();

                // Convert to list to execute the query, capture the result for re-use
                int numToSkip = (page - 1) * _count;
                IEnumerable<OutgoingMessageItem> outgoingMessages = outgoingMessagesQuery.OrderBy((message) => message.CreatedDate).Skip(numToSkip).Take(_count);

                // This uses the general FHIR parser and then sees if the json is a Bundle of BaseMessage Type
                // this will improve performance and prevent vague failures on the server, clients will be responsible for identifying incorrect messages
                IEnumerable<System.Threading.Tasks.Task<VR.CommonMessage>> messageTasks = outgoingMessages.Select(message => System.Threading.Tasks.Task.Run(() => CommonMessage.ParseGenericMessage(message.Message, true)));

                // create bundle to hold the response
                Bundle responseBundle = new Bundle();
                responseBundle.Type = Bundle.BundleType.Searchset;
                responseBundle.Timestamp = DateTime.Now;
                // Note that total is total number of matching results, not number being returned (outgoingMessages.Count)
                responseBundle.Total = totalMessageCount;
                // For the usual use case (unread only), the "next" page is just a repeated request.
                // But when using since, we have to actually track pages
                string baseUrl = GetNextUri();
                if (!additionalParamsProvided)
                {
                    // Only show the next link if there are additional messages beyond the current message set
                    if (totalMessageCount > outgoingMessages.Count())
                    {
                        responseBundle.NextLink = new Uri(baseUrl + Url.Action("GetOutgoingMessageItems", searchParamValues));
                    }
                }
                else
                {
                    var sinceFmt = _since.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
                    searchParamValues.Add("_since", sinceFmt);
                    searchParamValues.Remove("page");
                    searchParamValues.Add("page", 1);
                    responseBundle.FirstLink = new Uri(baseUrl + Url.Action("GetOutgoingMessageItems", searchParamValues));
                    // take the total number of the original selected messages, round up, and divide by the count to get the total number of pages
                    int lastPage = (outgoingMessagesQuery.Count() + (_count - 1)) / _count;
                    searchParamValues.Remove("page");
                    searchParamValues.Add("page", lastPage);
                    responseBundle.LastLink = new Uri(baseUrl + Url.Action("GetOutgoingMessageItems", searchParamValues));
                    if (page < lastPage)
                    {
                        searchParamValues.Remove("page");
                        searchParamValues.Add("page", page + 1);
                        responseBundle.NextLink = new Uri(baseUrl + Url.Action("GetOutgoingMessageItems", searchParamValues));
                    }
                }
                var messages = await System.Threading.Tasks.Task.WhenAll(messageTasks);
                DateTime retrievedTime = DateTime.UtcNow;
                // update each outgoing message's RetrievedAt field
                foreach(OutgoingMessageItem msgItem in outgoingMessages) {
                    MarkAsRetrieved(msgItem, retrievedTime);
                }

                // Add messages to the bundle
                foreach (var message in messages)
                {
                    responseBundle.AddResourceEntry((Bundle)message, "urn:uuid:" + message.MessageId);
                }

                _context.SaveChanges();
                return responseBundle;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An exception occurred while retrieving the response messages: {ex}");
                return StatusCode(500);
            }
        }

        // Allows overriding by STEVE controller to filter off different field
        /// <summary>
        /// Applies a filter (e.g. calls Where) to reduce the source to unretrieved messages. Should NOT iterate result set/execute query
        /// </summary>
        protected virtual IQueryable<OutgoingMessageItem> ExcludeRetrieved(IQueryable<OutgoingMessageItem> source)
        {
            return source.Where(message => message.RetrievedAt == null);
        }

        // Allows overriding by STEVE controller to mark different field
        protected virtual void MarkAsRetrieved(OutgoingMessageItem omi, DateTime retrieved)
        {
            omi.RetrievedAt = retrieved;
        }

        // POST: Bundles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        /// <summary>
        /// Submits a FHIR message to the API for processing
        /// </summary>
        /// <returns>If a single FHIR Message was submitted, nothing is returned. If a batch Bundle was submitted, a batch response is returned.</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST
        ///
        ///     {
        ///         "resourceType": "Bundle",
        ///         "id": "bffdbf2e-c0db-49cf-9f52-59a6459635b9",
        ///         "type": "message",
        ///         "timestamp": "2022-07-27T15:30:39.5787234+00:00",
        ///         "entry": [
        ///             { ...
        ///             }
        ///         ]
        ///     }
        ///
        /// </remarks>
        /// <response code="204">Content created</response>
        /// <response code="400">Bad Request</response>
        /// <response code="500">Internal Error, token may have expired</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Bundle>> PostIncomingMessageItem(string jurisdictionId, string vitalType, string igVersion, [FromBody] object text, [FromServices] IBackgroundTaskQueue queue)
        {
            if (!VR.IJEData.Instance.JurisdictionCodes.ContainsKey(jurisdictionId))
            {
                // Don't log the jurisdictionId value itself, since it is (known-invalid) user input
                _logger.LogError("Rejecting request with invalid jurisdiction ID.");
                return BadRequest("Invalid jurisdiction ID");
            }
            if (!ValidateVitalTypeIGVersion(ref vitalType, ref igVersion, out BadRequestObjectResult br))
            {
                return br;
            }

            // Check page 35 of the messaging document for full flow
            // Change over to 1 entry in the database per message
            Bundle responseBundle = new Bundle();
            try
            {

                Bundle bundle = CommonMessage.ParseGenericBundle(text.ToString(), true);
                // check whether the bundle is a message or a batch
                if (bundle?.Type == Bundle.BundleType.Batch)
                {
                    responseBundle = new Bundle();
                    responseBundle.Type = Bundle.BundleType.BatchResponse;
                    responseBundle.Timestamp = DateTime.Now;

                    // For Batch Processing: 
                    // Process each entry as an individual BaseMessage.
                    // One invalid message should not prevent the successful submission 
                    // of a separate, valid message in the bundle.
                    // Capture the each messsage's result in an entry and add to the response bundle.
                    foreach (var entry in bundle.Entry)
                    {
                        Bundle.EntryComponent respEntry = await InsertBatchMessage(entry, jurisdictionId, vitalType, igVersion, queue);
                        responseBundle.Entry.Add(respEntry);
                    }
                    return responseBundle;
                }
                else
                {

                    IncomingMessageItem item;
                    try
                    {
                        item = ParseIncomingMessageItem(jurisdictionId, vitalType, igVersion, bundle);
                        // Send a special message for extraction errors to report the error manually
                        if (item.MessageType == nameof(ExtractionErrorMessage))
                        {
                            _logger.LogDebug($"Error: Unsupported message type vrdr_extraction_error found");
                            return BadRequest($"Unsupported message type: NCHS API does not accept extraction errors. Please report extraction errors to NCHS manually.");
                        }
                        if (item.MessageType == nameof(BirthRecordErrorMessage))
                        {
                            _logger.LogDebug($"Error: Unsupported message type bfdr_extraction_error found");
                            return BadRequest($"Unsupported message type: NCHS API does not accept extraction errors. Please report extraction errors to NCHS manually.");
                        }
                        // check this is a valid message type
                        // submission message
                        // update message
                        // void message
                        // alias message
                        // acknowledgement message
                        if (!(birthMessageType(item.MessageType) || fetalDeathMessageType(item.MessageType) || vrdrMessageType(item.MessageType)))
                        {
                            _logger.LogDebug($"Error: Unsupported message type {item.MessageType} found");
                            return BadRequest($"Unsupported message type: NCHS API does not accept messages of type {item.MessageType}");
                        }
                    }
                    catch (VR.MessageRuleException mrx)
                    {
                        _logger.LogDebug($"Rejecting message with invalid message header: {mrx}");
                        return BadRequest($"Message was missing reqiured header fields: {mrx.Message}");
                    }
                    catch (ArgumentException aEx)
                    {
                        _logger.LogDebug($"Rejecting message with missing required field: {aEx}");
                        return BadRequest($"Message was missing required field: {aEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"An exception occurred while parsing the incoming message: {ex}");
                        return BadRequest("Failed to parse message. Please verify that it is consistent with the current Vital Records Messaging FHIR Implementation Guide.");
                    }

                    item.Source = GetMessageSource();

                    try
                    {
                        await SaveIncomingMessageItem(item, queue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"An exception occurred while saving the incoming message: {ex}");
                        return StatusCode(500);
                    }
                }

                // return HTTP status code 204 (No Content)
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An exception occurred while parsing the incoming bundle: {ex}");
                return BadRequest("Failed to parse bundle. Please verify that it is consistent with the current Vital Records Messaging FHIR Implementation Guide.");
            }
        }

        private bool ValidateJurisdictionId(string messageJurisdictionId, string urlParamJurisdictionId) {
            if (messageJurisdictionId == null)
            {
                _logger.LogError("Rejecting request without a jurisdiction ID in submission.");
                return false;
            }
            if (!messageJurisdictionId.Equals(urlParamJurisdictionId))
            {
                _logger.LogError("Rejecting request with non-matching jurisidtion IDs: Message jurisdiction ID [" + messageJurisdictionId + "] and parameter jurisdiction ID [" + urlParamJurisdictionId + "].");
                return false;
            }
            return true;
        }

        private static bool ValidateIGPayloadVersion(string messagePayloadVersion, string urlParamIGVersion)
        {
            // If the message payload is not provided, as in from an old version of messaging, then the url param must be v2.2.
            if (String.IsNullOrEmpty(messagePayloadVersion) && urlParamIGVersion == "v2.2")
            {
                return true;
            }
            Dictionary<string, string> validVersionMappings = new()
            {
              { "BFDR_STU2_0", "v2.0" },
              { "VRDR_STU3_0", "v3.0" },
              { "VRDR_STU2_2", "v2.2" }
            };
            return messagePayloadVersion != null && validVersionMappings.ContainsKey(messagePayloadVersion) && validVersionMappings[messagePayloadVersion].Equals(urlParamIGVersion);
        }

        // InsertBatchMessage handles a single message in a batch upload submission
        // Each message is handled independent of the other messages. A status code is generated for
        // each message and is returned in the response bundle
        private async Task<Bundle.EntryComponent> InsertBatchMessage(Bundle.EntryComponent msgBundle, string jurisdictionId, string vitalType, string igVersion, IBackgroundTaskQueue queue)
        {
            Bundle.EntryComponent entry = new Bundle.EntryComponent();
            IncomingMessageItem item;

            try
            {
                //BaseMessage message = BaseMessage.Parse<BaseMessage>((Hl7.Fhir.Model.Bundle)msgBundle.Resource);
                // get the bundle in the bundle
                Bundle bundle = (Hl7.Fhir.Model.Bundle)msgBundle.Resource;
                item = ParseIncomingMessageItem(jurisdictionId, vitalType, igVersion, bundle);
                if (item.MessageType == "ExtractionErrorMessage")
                {
                    _logger.LogDebug($"Error: Unsupported message type vrdr_extraction_error found");
                    entry.Response = new Bundle.ResponseComponent();
                    entry.Response.Status = "400";
                    entry.Response.Outcome = OperationOutcome.ForMessage($"Unsupported message type: NCHS API does not accept extraction errors. Please report extraction errors to NCHS manually.", OperationOutcome.IssueType.Exception);
                    return entry;
                }
                if (item.MessageType == nameof(BirthRecordErrorMessage))
                {
                    _logger.LogDebug($"Error: Unsupported message type bfdr_extraction_error found");
                    entry.Response = new Bundle.ResponseComponent();
                    entry.Response.Status = "400";
                    entry.Response.Outcome = OperationOutcome.ForMessage($"Unsupported message type: NCHS API does not accept extraction errors. Please report extraction errors to NCHS manually.", OperationOutcome.IssueType.Exception);
                    return entry;
                }
                if (!(birthMessageType(item.MessageType) || fetalDeathMessageType(item.MessageType) || vrdrMessageType(item.MessageType)))
                {
                    _logger.LogDebug($"Error: Unsupported message type {item.MessageType} found");
                    entry.Response = new Bundle.ResponseComponent();
                    entry.Response.Status = "400";
                    entry.Response.Outcome = OperationOutcome.ForMessage($"Unsupported message type: NCHS API does not accept messages of type {item.MessageType}", OperationOutcome.IssueType.Exception);
                    return entry;
                }
            }
            catch (VR.MessageRuleException mrx)
            {
                _logger.LogDebug($"Rejecting message with invalid message header: {mrx}");
                entry.Response = new Bundle.ResponseComponent();
                entry.Response.Status = "400";
                entry.Response.Outcome = OperationOutcome.ForMessage($"Message was missing required header field. {mrx.Message}.", OperationOutcome.IssueType.Exception);
                return entry;
            }
            catch (ArgumentException aEx)
            {
                _logger.LogDebug($"An exception occurred while parsing the incoming message: {aEx}");
                entry.Response = new Bundle.ResponseComponent();
                entry.Response.Status = "400";
                entry.Response.Outcome = OperationOutcome.ForMessage($"Message was missing required field. {aEx.Message}.", OperationOutcome.IssueType.Exception);
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An exception occurred while parsing the incoming message: {ex}");
                entry.Response = new Bundle.ResponseComponent();
                entry.Response.Status = "400";
                entry.Response.Outcome = OperationOutcome.ForMessage("Failed to parse message. Please verify that it is consistent with the current Vital Records Messaging FHIR Implementation Guide.", OperationOutcome.IssueType.Exception);
                return entry;
            }

            item.Source = GetMessageSource();
            try
            {
                await SaveIncomingMessageItem(item, queue);
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An error occurred while saving the incoming message: {ex}");
                entry.Response = new Bundle.ResponseComponent();
                entry.Response.Status = "500";
                entry.Response.Outcome = OperationOutcome.ForMessage("An error occurred while saving the incoming message", OperationOutcome.IssueType.Exception);
                return entry;
            }

            entry.Response = new Bundle.ResponseComponent();
            entry.Response.Status = "201";
            return entry;
        }

        /// <summary>
        /// Get the value to use for the message Source (default is SAM). ALlows override by STEVE endpoint.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetMessageSource()
        {
            return "SAM";
        }

        /// <summary>
        /// Get the value to use for the message Next Link for pagination (default is SAM). ALlows override by STEVE endpoint.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetNextUri()
        {
            return (_settings.SAMS);
        }

        protected IncomingMessageItem ParseIncomingMessageItem(string jurisdictionId, string vitalType, string igVersion, Bundle bundle)
        {
            try
            {
                CommonMessage message;
                vitalType = vitalType?.ToUpper();
                if (_settings.BirthEnabled && !String.IsNullOrEmpty(vitalType) && vitalType.Equals("BFDR-BIRTH"))
                {
                    message = BFDRBaseMessage.Parse(bundle);
                }
                else if (_settings.FetalDeathEnabled && !String.IsNullOrEmpty(vitalType) && vitalType.Equals("BFDR-FETALDEATH"))
                {
                    message = BFDRBaseMessage.Parse(bundle);
                }
                else if (vitalType.Equals("VRDR"))
                {
                    message = BaseMessage.Parse(bundle);
                }
                else
                {
                    throw new ArgumentException($"Record type url must be of 'VRDR', 'BFDR-BIRTH', or 'BFDR-FETALDEATH' but given {vitalType}. If using BFDR, check that BFDR is enabled in settings.");
                }
                CommonMessage.ValidateMessageHeader(message);
                return ValidateAndCreateIncomingMessageItem(message, jurisdictionId, igVersion);
            }
            catch (VR.MessageRuleException mrx)
            {
                throw mrx;
            }
            catch (BFDR.MessageParseException mpex) 
            { 
                _logger.LogDebug($"The message could not be parsed as a BFDR message. {mpex}");
                throw new ArgumentException($"Failed to parse input as a BFDR message, message type unrecognized.");
            }
            catch (VRDR.MessageParseException mpex) 
            { 
                _logger.LogDebug($"The message could not be parsed as a VRDR message. {mpex}");
                throw new ArgumentException($"Failed to parse input as a VRDR message, message type unrecognized.");
            }
            catch (ArgumentException aex)
            {
                throw aex;
            }

            throw new ArgumentException($"Failed to parse input as a VRDR or BFDR message, message type unrecognized.");
        }

        protected IncomingMessageItem ValidateAndCreateIncomingMessageItem(CommonMessage message, string jurisdictionId, string igVersion)
        {
            // Pre-check some minimal requirements for validity. Specifically, if there are problems with the message that will lead to failure when
            // attempting to insert into the database (e.g. missing MessageId), catch that here to return a 400 instead of a 500 on DB error
            // Message errors SHOULD result in an ExtractionError response; this check is just to catch things that can't make it that far
            if (String.IsNullOrWhiteSpace(message.MessageSource))
            {
                _logger.LogDebug($"Message is missing source endpoint, throw exception");
                throw new ArgumentException("Message source endpoint cannot be null");
            }
            if (String.IsNullOrWhiteSpace(message.MessageDestination))
            {
                _logger.LogDebug($"Message is missing destination endpoint, throw exception");
                throw new ArgumentException("Message destination endpoint cannot be null");
            }
            if (!validateNCHSDestination(message.MessageDestination))
            {
                _logger.LogDebug($"Message destination endpoint does not include a valid NCHS endpoint, throw exception");
                throw new ArgumentException("Message destination endpoint does not include a valid NCHS endpoint");
            }
            if (String.IsNullOrWhiteSpace(message.MessageId))
            {
                _logger.LogDebug($"Message is missing Message ID, throw exception");
                throw new ArgumentException("Message ID cannot be null");
            }
            if (String.IsNullOrWhiteSpace(message.GetType().Name))
            {
                _logger.LogDebug($"Message is missing Message Event Type, throw exception");
                throw new ArgumentException("Message Event Type cannot be null");
            }
            if (message.CertNo == null)
            {
                _logger.LogDebug($"Message is missing Certificate Number, throw exception");
                throw new ArgumentException("Message Certificate Number cannot be null");
            }
            if ((uint)message.CertNo.ToString().Length > 6)
            {
                _logger.LogDebug($"Message Certificate Number number is greater than 6 characters, throw exception");
                throw new ArgumentException("Message Certificate Number cannot be more than 6 digits long");
            }
            if (!ValidateJurisdictionId(message.JurisdictionId, jurisdictionId))
            {
                _logger.LogDebug($"The message resource jurisdiction ID {message.JurisdictionId} did not match the parameter jurisdiction ID {jurisdictionId}.");
                throw new ArgumentException($"Message jurisdiction ID {message.JurisdictionId} must match the URL parameter jurisdiction ID {jurisdictionId}.");
            }
            if (!ValidateIGPayloadVersion(message.PayloadVersionId, igVersion))
            {
                _logger.LogDebug($"The message resource Payload Version {message.PayloadVersionId} did not match the parameter IG Version {igVersion}.");
                throw new ArgumentException($"Message Payload Version {message.PayloadVersionId} must match the URL parameter IG Version {igVersion}.");
            }
            IncomingMessageItem item = new IncomingMessageItem();
            item.Message = message.ToJSON(); 
            item.MessageId = message.MessageId;
            item.MessageType = message.GetType().Name;
            item.JurisdictionId = jurisdictionId;
            if(birthMessageType(message.GetType().Name) || fetalDeathMessageType(message.GetType().Name))
            {
                item.EventYear = ((BFDRBaseMessage)message).EventYear;
            }
            else if(vrdrMessageType(message.GetType().Name))
            {
                item.EventYear = message.GetYear();
            }

            // format the certificate number
            uint certNo = (uint)message.CertNo;
            string certNoFmt = certNo.ToString("D6");
            item.CertificateNumber = certNoFmt;
            
            item.EventType = getEventType(message);

            return item;
        }

        protected async System.Threading.Tasks.Task SaveIncomingMessageItem(IncomingMessageItem item, IBackgroundTaskQueue queue)
        {
            await _context.IncomingMessageItems.AddAsync(item);
            await _context.SaveChangesAsync();

            // Queue Natality messages for auto responses while Natality is in dev, and queue all messages if AckAndIJEConversion is "on" for testing
            // For the June 2025 test event, enable auto responses for Fetal death message types only
            if (_settings.AckAndIJEConversion || ( _settings.FetalDeathEnabled && item.EventType == "FET"))
            {
                queue.QueueConvertToIJE(item.Id);
            }
            // If we are in test mode, give the worker thread 1 extra second to insert the outgoing message, this helps our tests avoid race condition failures
            if (_settings.AckAndIJEConversion)
            {
                Thread.Sleep(new TimeSpan(0,0,1));
            }
        }

        // getEventType generates an EventType string "MOR", "NAT", or "FET"
        // for debugging and tracking records in the db
        private string getEventType(CommonMessage message)
        {
            switch (message.MessageType)
            {
                case "http://nchs.cdc.gov/vrdr_acknowledgement":
                case "http://nchs.cdc.gov/vrdr_alias":
                case "http://nchs.cdc.gov/vrdr_causeofdeath_coding":
                case "http://nchs.cdc.gov/vrdr_causeofdeath_coding_update":
                case "http://nchs.cdc.gov/vrdr_demographics_coding":
                case "http://nchs.cdc.gov/vrdr_demographics_coding_update":
                case "http://nchs.cdc.gov/vrdr_extraction_error":
                case "http://nchs.cdc.gov/vrdr_status":
                case "http://nchs.cdc.gov/vrdr_submission":
                case "http://nchs.cdc.gov/vrdr_submission_update":
                case "http://nchs.cdc.gov/vrdr_submission_void":
                    return "MOR";
                case "http://nchs.cdc.gov/birth_submission":
                case "http://nchs.cdc.gov/birth_acknowledgement":
                case "http://nchs.cdc.gov/birth_submission_update": 
                case "http://nchs.cdc.gov/birth_demographics_coding":
                case "http://nchs.cdc.gov/birth_extraction_error":
                case "http://nchs.cdc.gov/birth_status":
                case "http://nchs.cdc.gov/birth_submission_void":
                    return "NAT";
                case "http://nchs.cdc.gov/fd_submission":
                case "http://nchs.cdc.gov/fd_acknowledgement":
                case "http://nchs.cdc.gov/fd_submission_update": 
                case "http://nchs.cdc.gov/fd_demographics_coding":
                case "http://nchs.cdc.gov/fd_extraction_error":
                case "http://nchs.cdc.gov/fd_status":
                case "http://nchs.cdc.gov/fd_submission_void":
                    return "FET";
                default:
                    return "UNK";
            }
        }

        // validateNCHSDestination checks that an NCHS destination is included
        // in the list of destinations
        private bool validateNCHSDestination(string destination)
        {
            // validate NCHS is in the list of destination endpoints
            List<string> destinationEndpoints = destination.Split(',').ToList();
            foreach (string d in destinationEndpoints)
            {
                // set the message destination to lowercase to make the url validation case-insensitive
                switch (d.ToLower())
                {
                    case "http://nchs.cdc.gov/vrdr_acknowledgement":
                    case "http://nchs.cdc.gov/vrdr_alias":
                    case "http://nchs.cdc.gov/vrdr_causeofdeath_coding":
                    case "http://nchs.cdc.gov/vrdr_causeofdeath_coding_update":
                    case "http://nchs.cdc.gov/vrdr_demographics_coding":
                    case "http://nchs.cdc.gov/vrdr_demographics_coding_update":
                    case "http://nchs.cdc.gov/vrdr_extraction_error":
                    case "http://nchs.cdc.gov/vrdr_status":
                    case "http://nchs.cdc.gov/vrdr_submission":
                    case "http://nchs.cdc.gov/vrdr_submission_update":
                    case "http://nchs.cdc.gov/vrdr_submission_void":
                    case "http://nchs.cdc.gov/bfdr_submission":
                    case "http://nchs.cdc.gov/bfdr_acknowledgement": 
                    case "http://nchs.cdc.gov/bfdr_demographics_coding":
                    case "http://nchs.cdc.gov/bfdr_extraction_error":
                    case "http://nchs.cdc.gov/bfdr_status":
                    case "http://nchs.cdc.gov/bfdr_submission_void":
                        return true;
                    default:
                        break;
                }
            }
            return false;
        }

        // Checks that the message type is accepted at NCHS for death
        private static bool vrdrMessageType(string messageType)
        {
            // set the message type to lowercase to make case-insensitive
            switch (messageType)
            {
                case "AcknowledgementMessage":
                case "DeathRecordAliasMessage":
                case "DeathRecordSubmissionMessage":
                case "DeathRecordUpdateMessage":
                case "DeathRecordVoidMessage":
                    return true;
                default:
                    break;
            }
            return false;
        }

        // Checks that the message type is accepted at NCHS for birth
        private static bool birthMessageType(string messageType)
        {
            // set the message type to lowercase to make case-insensitive
            switch (messageType)
            {
                case "BirthRecordSubmissionMessage":
                case "BirthRecordUpdateMessage":
                case "BirthRecordAcknowledgementMessage": 
                case "BirthRecordVoidMessage":
                    return true;
                default:
                    break;
            }
            return false;
        }

        // Checks that the message type is accepted at NCHS for featl death
        private static bool fetalDeathMessageType(string messageType)
        {
            // set the message type to lowercase to make case-insensitive
            switch (messageType)
            {
                case "FetalDeathRecordSubmissionMessage":
                case "FetalDeathRecordUpdateMessage":
                case "FetalDeathRecordAcknowledgementMessage": 
                case "FetalDeathRecordVoidMessage":
                    return true;
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// Validate that the Vital Type and IG Version are valid together. If null data is provided, they default to VRDR/v2.2
        /// </summary>
        /// <param name="vitalType"></param>
        /// <param name="igVersion"></param>
        /// <param name="br"></param>
        /// <returns></returns>
        protected bool ValidateVitalTypeIGVersion(ref string vitalType, ref string igVersion, out BadRequestObjectResult br)
        {
            if (String.IsNullOrEmpty(vitalType) && String.IsNullOrEmpty(igVersion))
            {
                // If the historical backwards compatibilty endpoint is used (no vital type or ig version provided), default to VRDR v2.0
                vitalType = "VRDR";
                igVersion = "v2.2";
            }
            else if (String.IsNullOrEmpty(vitalType))
            {
                _logger.LogError($"Rejecting request with invalid url path.");
                br = BadRequest("Invalid url path provided");
                return false;
            }
            else if ((!String.IsNullOrEmpty(vitalType) && String.IsNullOrEmpty(igVersion)) || (String.IsNullOrEmpty(vitalType) && !String.IsNullOrEmpty(igVersion)))
            {
                _logger.LogError($"Rejecting request with invalid url path When either vital type or ig version are provided, both must be provided. Vital type: {vitalType}, IG Version: {igVersion}");
                br = BadRequest("Invalid url path provided");
                return false;
            }
            else if (vitalType.Equals("BFDR-BIRTH") && !_settings.BirthEnabled)
            {
                _logger.LogError("Rejecting request for natality data. BFDR Birth messaging is not enabled.");
                br = BadRequest("BFDR Birth messaging is not enabled.");
                return false;
            }
            else if (vitalType.Equals("BFDR-FETALDEATH") && !_settings.FetalDeathEnabled)
            {
                _logger.LogError("Rejecting request for natality data. BFDR Fetal Death messaging is not enabled.");
                br = BadRequest("BFDR Fetal Death messaging is not enabled.");
                return false;
            }
            if (!ValidIGVersion(vitalType, igVersion))
            {
                _logger.LogError($"Rejecting request with invalid url path. Vital type or ig version are invalid. Vital type: {vitalType}, IG Version: {igVersion}");
                br = BadRequest("Invalid url path provided");
                return false;
            }
            br = null;
            return true;
        }

        /// <summary>
        /// Return true if the vitalType and igVersion align with a valid IG version.
        /// </summary>
        /// <param name="vitalType"></param>
        /// <param name="igVersion"></param>
        /// <returns></returns>
        private static bool ValidIGVersion(string vitalType, string igVersion)
        {
            string[] BFDRIgs = {"v2.0"};
            string[] VRDRIgs = {"v2.2", "v3.0"};

            if (vitalType == "BFDR-BIRTH" || vitalType == "BFDR-FETALDEATH")
            {
                return BFDRIgs.Contains(igVersion);
            }
            else if (vitalType == "VRDR")
            {
                return VRDRIgs.Contains(igVersion);
            }
            return false;
        }
    }
}
