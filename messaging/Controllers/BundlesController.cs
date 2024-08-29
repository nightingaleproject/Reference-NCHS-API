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
using System.Linq.Expressions;
using Hl7.Fhir.Utility;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace messaging.Controllers
{
    [Route("{jurisdictionId:length(2)}/Bundle")]
    [Route("{jurisdictionId:length(2)}/Bundle/{vitalType:length(4)}/{igVersion}")]
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

            // if a vitalType is provided in the url, it must be a valid vital type and ig version
            if (!String.IsNullOrEmpty(vitalType) && !validIGVersion(vitalType, igVersion))
            {
                _logger.LogError($"Rejecting request with invalid url path. {vitalType}");
                return BadRequest("Invalid url path provided");
            }

            string recordType = "";
            if (!String.IsNullOrEmpty(vitalType))
            {
                if (vitalType.Equals("VRDR"))
                {
                    recordType = "MOR";
                }
                else if (vitalType.Equals("BFDR"))
                {
                    if (_settings.BFDREnabled)
                    {
                        recordType = "NAT";
                    }
                    else
                    {
                        _logger.LogError("Rejecting request for natality data. BFDR messaging is not enabled.");
                        return BadRequest("BFDR messaging is not enabled.");
                    }

                }
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

            // Query for outgoing messages by jurisdiction ID. Filter by certificate number and death year if those parameters are provided.
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
            // if a vitalType is provided in the url, it must be a valid vital type and ig version
            if (!String.IsNullOrEmpty(vitalType) && !validIGVersion(vitalType, igVersion))
            {
                _logger.LogError($"Rejecting request with invalid url path. {vitalType}");
                return BadRequest("Invalid url, IG version was not recognized");
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
                        item = ParseIncomingMessageItem(jurisdictionId, vitalType, bundle);
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
                        if (!(bfdrMessageType(item.MessageType) || vrdrMessageType(item.MessageType)))
                        {
                            _logger.LogDebug($"Error: Unsupported message type {item.MessageType} found");
                            return BadRequest($"Unsupported message type: NCHS API does not accept messages of type {item.MessageType}");
                        }

                    }
                    catch (VRDR.MessageParseException ex)
                    {
                        _logger.LogDebug($"A message parsing exception occurred while parsing the incoming message: {ex}");
                        return BadRequest($"Failed to parse message: {ex.Message}. Please verify that it is consistent with the current Vital Records Messaging FHIR Implementation Guide.");
                    }
                    catch (MessageRuleException mrx)
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
                item = ParseIncomingMessageItem(jurisdictionId, vitalType, bundle);
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
                if (!(bfdrMessageType(item.MessageType) || vrdrMessageType(item.MessageType)))
                {
                    _logger.LogDebug($"Error: Unsupported message type {item.MessageType} found");
                    entry.Response = new Bundle.ResponseComponent();
                    entry.Response.Status = "400";
                    entry.Response.Outcome = OperationOutcome.ForMessage($"Unsupported message type: NCHS API does not accept messages of type {item.MessageType}", OperationOutcome.IssueType.Exception);
                    return entry;
                }
            }
            // catch (VRDR.MessageParseException ex)
            // {
            //     _logger.LogDebug($"A message parsing exception occurred while parsing the incoming message: {ex}");
            //     entry.Response = new Bundle.ResponseComponent();
            //     entry.Response.Status = "400";
            //     entry.Response.Outcome = OperationOutcome.ForMessage($"Failed to parse message: {ex.Message}. Please verify that it is consistent with the current Vital Records Messaging FHIR Implementation Guide.", OperationOutcome.IssueType.Exception);
            //     return entry;
            // }
            catch (MessageRuleException mrx)
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

        protected IncomingMessageItem ParseIncomingMessageItem(string jurisdictionId, string vitalType, Bundle bundle)
        {
            //CommonMessage message;
            // try parsing as a bfdr message if the vitalType is either unknown or explicitly BFDR
            if (_settings.BFDREnabled)
            {
                if (String.IsNullOrEmpty(vitalType) || vitalType.Equals("BFDR") )
                {
                    try
                    {
                        CommonMessage message = BirthRecordBaseMessage.Parse(bundle);
                        return ValidateAndCreateIncomingMessageItem(message, jurisdictionId);
                    }
                    catch(BFDR.MessageParseException ex)
                    {
                        _logger.LogDebug($"The message could not be parsed as a bfdr message. Trying to parse as a vrdr message...");
                    }
                    catch(ArgumentException aex)
                    {
                        // pass exception thrown by our validation to calling function
                        throw aex;
                    }
                }
            }



            // try parsing as a vrdr message if the vitalType is either unknown or explicitly VRDR
            if (String.IsNullOrEmpty(vitalType) || vitalType.Equals("VRDR") )
            {
                try
                {
                    BaseMessage message = BaseMessage.Parse(bundle);
                    BaseMessage.ValidateMessageHeader(message);
                    return ValidateAndCreateIncomingVRDRMessageItem(message, jurisdictionId);
                }
                catch(MessageRuleException mrx)
                {   
                    throw mrx;
                }
                catch(VRDR.MessageParseException ex)
                {
                    _logger.LogDebug($"The message could not be parsed as a vrdr message: {ex}");
                }
                catch(ArgumentException aex)
                {
                    // pass exception thrown by our validation to calling function
                    throw aex;
                }
            }



            // if we make it here, the message wasn't parsed successfully, throw an error
            _logger.LogDebug($"Message type not recognized, throw exception");
            throw new ArgumentException("Failed to parse message, message type unrecognized");
        }

        protected IncomingMessageItem ValidateAndCreateIncomingMessageItem(CommonMessage message, string jurisdictionId)
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

            IncomingMessageItem item = new IncomingMessageItem();
            item.Message = message.ToJSON(); 
            item.MessageId = message.MessageId;
            item.MessageType = message.GetType().Name;
            item.JurisdictionId = jurisdictionId;
            if(bfdrMessageType(message.GetType().Name))
            {
                item.EventYear = ((BirthRecordBaseMessage)message).BirthYear;
            }
            // TODO add this check once we move to the monorepo version of VRDR STU 3.0
            // else if(vrdrMessageType(message.GetType().Name))
            // {
            //     item.EventYear = ((BaseMessage)message).DeathYear;
            // }

            // format the certificate number
            uint certNo = (uint)message.CertNo;
            string certNoFmt = certNo.ToString("D6");
            item.CertificateNumber = certNoFmt;
            
            item.EventType = getEventType(message);

            return item;
        }

        // TODO: Once we move to VRDR STU 3.0 and the monorepo version of VRDR, we can delete this function
        protected IncomingMessageItem ValidateAndCreateIncomingVRDRMessageItem(BaseMessage message, string jurisdictionId)
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

            IncomingMessageItem item = new IncomingMessageItem();
            item.Message = message.ToJSON(); 
            item.MessageId = message.MessageId;
            item.MessageType = message.GetType().Name;
            item.JurisdictionId = jurisdictionId;
            item.EventYear = message.DeathYear;

            // format the certificate number
            uint certNo = (uint)message.CertNo;
            string certNoFmt = certNo.ToString("D6");
            item.CertificateNumber = certNoFmt;
            
            item.EventType = "MOR";

            return item;
        }

        protected async System.Threading.Tasks.Task SaveIncomingMessageItem(IncomingMessageItem item, IBackgroundTaskQueue queue)
        {
            await _context.IncomingMessageItems.AddAsync(item);
            await _context.SaveChangesAsync();

            // Queue Natality messages for auto responses while Natality is in dev, and queue all messages if AckAndIJEConversion is "on" for testing
            if (_settings.AckAndIJEConversion)
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
        // For now we only have "MOR" records but we could add "NAT" and "FET" here later
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
                case "http://nchs.cdc.gov/bfdr_submission":
                case "http://nchs.cdc.gov/bfdr_acknowledgement":
                case "http://nchs.cdc.gov/bfdr_submission_update": 
                case "http://nchs.cdc.gov/bfdr_demographics_coding":
                case "http://nchs.cdc.gov/bfdr_extraction_error":
                case "http://nchs.cdc.gov/bfdr_status":
                case "http://nchs.cdc.gov/bfdr_submission_void":
                    return "NAT";
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

        // validateMessageType checks that the message type is accepted at NCHS
        private bool vrdrMessageType(string messageType)
        {
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
            }
            return false;
        }

        // validateMessageType checks that the message type is accepted at NCHS
        private bool bfdrMessageType(string messageType)
        {
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
            }
            return false;
        }

        // validIGVersion will return true if the vitalType and igVersion align with a real IG STU version
        private bool validIGVersion(string vitalType, string igVersion)
        {
            string[] BFDRIgs = {"v2.0"};
            string[] VRDRIgs = {"v2.2"};

            if (vitalType == "BFDR")
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
