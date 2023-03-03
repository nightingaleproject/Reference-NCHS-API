# URL Validation
URL validation applies to all incoming requests.
- Validates that the Jurisdiction Code parameter is in the known set
```
            if (!VRDR.MortalityData.Instance.JurisdictionCodes.ContainsKey(jurisdictionId))
            {
                // Don't log the jurisdictionId value itself, since it is (known-invalid) user input
                _logger.LogError("Rejecting request with invalid jurisdiction ID.");
                return BadRequest();
            }
```
# Get Requests
Parameter validation for `count`, `page`, and `_since`
- Validates that `count` isn't negative
- Validates `page` is greater than 0
- Requires a `_since` parameter if a `page` parameter other than 1 is provided
```
    public async Task<ActionResult<Bundle>> GetOutgoingMessageItems(string jurisdictionId, int _count, DateTime _since = default(DateTime), int page = 1)
    {
        if (_count == 0)
        {
            _count = _settings.PageCount;
        }

        if (!VRDR.MortalityData.Instance.JurisdictionCodes.ContainsKey(jurisdictionId))
        {
            // Don't log the jurisdictionId value itself, since it is (known-invalid) user input
            _logger.LogError("Rejecting request with invalid jurisdiction ID.");
            return BadRequest();
        }

        if (_count < 0)
        {
            return BadRequest("_count must not be negative");
        }
        if (page < 1)
        {
            return BadRequest("page must not be negative");
        }
        // Retrieving unread messages changes the result set (as they get marked read), so we don't REALLY support paging
        if (_since == default(DateTime) && page > 1)
        {
            return BadRequest("Pagination does not support specifying a page without a _since parameter");
        }
        ...
```

# POST Requests
Minimal validation is done on the bundle to avoid complexity at the API level. However, we want to make sure the message is parsable and traceable if it will be added to the database for ITB. It must be parsable, have the required headers, and be a valid event type.
  
## Parsing Validation
- Validate we can parse a basic bundle
- Validate we can parse generic message for each entry in the budle
```
    Bundle bundle = BaseMessage.ParseGenericBundle(text.ToString(), true);
```
```
    BaseMessage message = BaseMessage.Parse<BaseMessage>((Hl7.Fhir.Model.Bundle)msgBundle.Resource);
```

## Message Header Field Validation
- Validate the required headers are provided
  - Required headers: MessageSource, MessageDestination, MessageId, Event Type, CertNo 
```
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
```
## Message Type Validation
- NCHS does not currently process Extraction Error Messages. Return 400 and provide a specific error message directing them to report the error manually.
- Validate the message Event Type is a valid type accepted at NCHS
  - Valid Event Types for NCHS bound messages: DeathRecordSubmissionMessage, DeathRecordUpdateMessage, DeathRecordVoidMessage, DeathRecordAliasMessage, AcknowledgementMessage
```
    if (item.MessageType == nameof(ExtractionErrorMessage))
    {
        _logger.LogDebug($"Error: Unsupported message type vrdr_extraction_error found");
        return BadRequest($"Unsupported message type: NCHS API does not accept extraction errors. Please report extraction errors to NCHS manually.");
    }

    if (item.MessageType != nameof(DeathRecordSubmissionMessage) && item.MessageType != nameof(DeathRecordUpdateMessage) && item.MessageType != nameof(DeathRecordVoidMessage) && item.MessageType != nameof(DeathRecordAliasMessage) && item.MessageType != nameof(AcknowledgementMessage))
    {
        _logger.LogDebug($"Error: Unsupported message type {item.MessageType} found");
        return BadRequest($"Unsupported message type: NCHS API does not accept messages of type {item.MessageType}");
    }
```
## Single Message Error Responses
Errors caught or generated in the checks listed above result in 400 with the following error messages.
```
    try
    {
        item = ParseIncomingMessageItem(jurisdictionId, text);
        // Send a special message for extraction errors to report the error manually
        if (item.MessageType == nameof(ExtractionErrorMessage))
        {
            _logger.LogDebug($"Error: Unsupported message type vrdr_extraction_error found");
            return BadRequest($"Unsupported message type: NCHS API does not accept extraction errors. Please report extraction errors to NCHS manually.");
        }
        // check this is a valid message type
        // submission message
        // update message
        // void message
        // alias message
        // acknowledgement message
        if (item.MessageType != nameof(DeathRecordSubmissionMessage) && item.MessageType != nameof(DeathRecordUpdateMessage) && item.MessageType != nameof(DeathRecordVoidMessage) && item.MessageType != nameof(DeathRecordAliasMessage) && item.MessageType != nameof(AcknowledgementMessage))
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
```
## Batch Message Error Responses
Errors caught or generated in the checks listed above result in 400 entry in the batch response with the following error messages.
```
    try
    {
        BaseMessage message = BaseMessage.Parse<BaseMessage>((Hl7.Fhir.Model.Bundle)msgBundle.Resource);
        item = ParseIncomingMessageItem(jurisdictionId, message.ToJSON());
        if (item.MessageType == "ExtractionErrorMessage")
        {
            _logger.LogDebug($"Error: Unsupported message type vrdr_extraction_error found");
            entry.Response = new Bundle.ResponseComponent();
            entry.Response.Status = "400";
            entry.Response.Outcome = OperationOutcome.ForMessage($"Unsupported message type: NCHS API does not accept extraction errors. Please report extraction errors to NCHS manually.", OperationOutcome.IssueType.Exception);
            return entry;
        }
        if (item.MessageType != nameof(DeathRecordSubmissionMessage) && item.MessageType != nameof(DeathRecordUpdateMessage) && item.MessageType != nameof(DeathRecordVoidMessage) && item.MessageType != nameof(DeathRecordAliasMessage) && item.MessageType != nameof(AcknowledgementMessage))
        {
            _logger.LogDebug($"Error: Unsupported message type {item.MessageType} found");
            entry.Response = new Bundle.ResponseComponent();
            entry.Response.Status = "400";
            entry.Response.Outcome = OperationOutcome.ForMessage($"Unsupported message type: NCHS API does not accept messages of type {item.MessageType}", OperationOutcome.IssueType.Exception);
            return entry;
        }
    }
    catch (VRDR.MessageParseException ex)
    {
        _logger.LogDebug($"A message parsing exception occurred while parsing the incoming message: {ex}");
        entry.Response = new Bundle.ResponseComponent();
        entry.Response.Status = "400";
        entry.Response.Outcome = OperationOutcome.ForMessage($"Failed to parse message: {ex.Message}. Please verify that it is consistent with the current Vital Records Messaging FHIR Implementation Guide.", OperationOutcome.IssueType.Exception);
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
```