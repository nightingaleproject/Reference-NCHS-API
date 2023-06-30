# Summary
This file documents the API's validation checks along with the returned error codes and messages.

# Get Requests

## Parameter Validation
Validation for the following URL parameters: `jurisdictionId` `_count` `page` `_since`

| Error Response Code | parameter | Validation Check | Error Message |
|-----|:------:|----------------|--------|
| 400 | `jurisdictionId` | `if !VRDR.MortalityData.Instance.JurisdictionCodes.ContainsKey(jurisdictionId)` | bad request: Invalid jurisdiction ID |
| 400 | `jurisdictionId` | `if !messageJurisdictionId.Equals(urlParamJurisdictionId)` | Message jurisdiction ID {message.JurisdictionId} must match the URL parameter jurisdiction ID {jurisdictionId}. |
| 400 | `_count` | `if _count < 0` | bad request: _count must not be negative |
| 400 | `page` | `if page < 1` | bad request: page must not be negative |
| 400 | `_since` | `if (_since == default(DateTime) && page > 1)` | bad request: Pagination does not support specifying a page without a _since parameter |
  
   
# POST Requests
Minimal validation is done on the bundle to avoid complexity at the API level. However, we want to make sure the message is parsable and traceable if it will be added to the database for ITB. It must be parsable, have the required headers, and be a valid event type.

## Parsing Validation

| Error Response Code | Validation Check | Error Message |
|------|----------------|--------|
| 400 | if parsing the generic bundle with `BaseMessage.ParseGenericBundle(text.ToString(), true);` throws an error | bad request: Failed to parse bundle. Please verify that it is consistent with the current Vital Records Messaging FHIR Implementation Guide. |
| 400 | if parsing a generic message with `BaseMessage.Parse<BaseMessage>((Hl7.Fhir.Model.Bundle)msgBundle.Resource);` throws an error | bad request: Failed to parse message: {ex.Message}. Please verify that it is consistent with the current Vital Records Messaging FHIR Implementation Guide. |


## Message Header Field Validation
Validates the required message headers are provided: `MessageSource`, `MessageDestination`, `MessageId`, `EventType`, `CertNo` 

| Error Response Code | Validation Check | Error Message |
|-----|----------------|--------|
| 400 | `if String.IsNullOrWhiteSpace(message.MessageSource)` | bad request: Message was missing required field: {aEx.Message} |
| 400 | `if String.IsNullOrWhiteSpace(message.MessageDestination)` | bad request: Message was missing required field: {aEx.Message} |
| 400 | `if String.IsNullOrWhiteSpace(message.MessageId)` | bad request: Message was missing required field: {aEx.Message} |
| 400 | `if String.IsNullOrWhiteSpace(message.GetType().Name)` | bad request: Message was missing required field: {aEx.Message} |
| 400 | `if message.CertNo == null` | bad request: Message was missing required field: {aEx.Message} |

## Message Type Validation
- Validates the message is not `ExtractionErrorMessage` messages since NCHS does not support them.  
- Validates the certificate number is no more than 6 characters.  
- Validates the message Event Type is a valid type accepted at NCHS: `DeathRecordSubmissionMessage`, `DeathRecordUpdateMessage`, `DeathRecordVoidMessage`, `DeathRecordAliasMessage`, or `AcknowledgementMessage`.  
- Validates the Destination Endpoint includes a valid nchs endpoint, this check is case insensitive: `http://nchs.cdc.gov/vrdr_acknowledgement`, `http://nchs.cdc.gov/vrdr_alias`, `http://nchs.cdc.gov/vrdr_causeofdeath_coding`, `http://nchs.cdc.gov/vrdr_causeofdeath_coding_update`, `http://nchs.cdc.gov/vrdr_demographics_coding`, `http://nchs.cdc.gov/vrdr_demographics_coding_update`, `http://nchs.cdc.gov/vrdr_extraction_error`, `http://nchs.cdc.gov/vrdr_status`, `http://nchs.cdc.gov/vrdr_submission`, `http://nchs.cdc.gov/vrdr_submission_update`, `http://nchs.cdc.gov/vrdr_submission_void`

| Error Response Code | Validation Check | Error Message |
|-----|----------------|--------|
| 400 | `if (item.MessageType == nameof(ExtractionErrorMessage)` | bad request: Unsupported message type: NCHS API does not accept extraction errors. Please report extraction errors to NCHS manually. |
| 400 | `if ((uint)message.CertNo.ToString().Length > 6)` | bad request: Message Certificate Number cannot be more than 6 digits long. |   
| 400 | `if (item.MessageType != nameof(DeathRecordSubmissionMessage) && item.MessageType != nameof(DeathRecordUpdateMessage) && item.MessageType != nameof(DeathRecordVoidMessage) && item.MessageType != nameof(DeathRecordAliasMessage) && item.MessageType != nameof(AcknowledgementMessage))` | bad request: Unsupported message type: NCHS API does not accept messages of type {item.MessageType} |
| 400 | `if (!validateNCHSDestination(message.MessageDestination))` | bad request: Message was missing required field: {aEx.Message} |

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
