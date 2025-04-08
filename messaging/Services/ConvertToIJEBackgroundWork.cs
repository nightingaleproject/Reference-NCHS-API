using BFDR;
using messaging.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VRDR;
using VR;
using System.Collections.Generic;

namespace messaging.Services
{
  public static class ConvertToIJEBackgroundWork
  {
      public static void QueueConvertToIJE(this IBackgroundTaskQueue queue, long messageId)
      {
          queue.QueueBackgroundWorkItemAsync(new Message(messageId));
      }

    public class Message : IBackgroundWorkOrder<Message, Worker>
    {
        // This is just the data to pass to the background thread.
        // No services in here, just a simple data object.
        public Message(long messageId)
        {
            this.Id = messageId;
        }

        public long Id { get; }
    }


      public class Worker : IBackgroundWorker<Message, Worker>
      {
        private readonly ApplicationDbContext _context;

        public Worker(ApplicationDbContext context)
        {
            // This is where you put your dependencies, services, etc.
            this._context = context;
        }

        private readonly static Dictionary<string, Action<CommonMessage, IncomingMessageItem, ApplicationDbContext>> CreateAckMessageCommand = new()
        {
            { "MOR", (message, dbMessage, _context) => CreateAckMessage(message, dbMessage, "MOR", m => new AcknowledgementMessage(m), _context) },
            { "NAT", (message, dbMessage, _context) => CreateAckMessage(message, dbMessage, "NAT", m => new BirthRecordAcknowledgementMessage(m), _context) },
            { "FET", (message, dbMessage, _context) => CreateAckMessage(message, dbMessage, "FET", m => new FetalDeathRecordAcknowledgementMessage(m), _context) }
        };

        private readonly static Dictionary<string, Func<CommonMessage, string>> CreateIJEStringCommand = new()
        {
            { "MOR", (message) => new IJEMortality(((DeathRecordSubmissionMessage) message).DeathRecord, false).ToString() },
            { "NAT", (message) => new IJEBirth(((BirthRecordSubmissionMessage) message).BirthRecord, false).ToString() },
            { "FET", (message) => new IJEFetalDeath(((FetalDeathRecordSubmissionMessage) message).FetalDeathRecord, false).ToString() }
        };

        public async Task DoWork(Message message, CancellationToken cancellationToken)
        {
            IncomingMessageItem item = this._context.IncomingMessageItems.Find(message.Id);
            item.ProcessedStatus = "PROCESSED";
            this._context.Update(item);
            OutgoingMessageItem outgoingMessageItem = new OutgoingMessageItem();
            outgoingMessageItem.JurisdictionId = item.JurisdictionId;
            outgoingMessageItem.IGVersion = item.IGVersion;
            CommonMessage parsedMessage;
            if (item.EventType == "MOR")
            {
                parsedMessage = BaseMessage.Parse(item.Message.ToString(), true);
            }
            else if (item.EventType == "NAT" || item.EventType == "FET")
            {
                parsedMessage = BFDRBaseMessage.Parse(item.Message.ToString(), true);
            }
            else
            {
                throw new Exception($"Invalid Event Type: {item.EventType}");
            }
            try
            {
                switch(parsedMessage) {
                    case DeathRecordUpdateMessage update:
                        HandleUpdateMessage(update, item, CreateIJEStringCommand[item.EventType], CreateAckMessageCommand[item.EventType]);
                        break;
                    case DeathRecordSubmissionMessage submission:
                        HandleSubmissionMessage(submission, item, CreateIJEStringCommand[item.EventType], CreateAckMessageCommand[item.EventType]);
                        break;
                    case BirthRecordUpdateMessage update:
                        HandleUpdateMessage(update, item, CreateIJEStringCommand[item.EventType], CreateAckMessageCommand[item.EventType]);
                        break;
                    case BirthRecordSubmissionMessage submission:
                        HandleSubmissionMessage(submission, item, CreateIJEStringCommand[item.EventType], CreateAckMessageCommand[item.EventType]);
                        break;
                    case FetalDeathRecordUpdateMessage update:
                        HandleUpdateMessage(update, item, CreateIJEStringCommand[item.EventType], CreateAckMessageCommand[item.EventType]);
                        break;
                    case FetalDeathRecordSubmissionMessage submission:
                        HandleSubmissionMessage(submission, item, CreateIJEStringCommand[item.EventType], CreateAckMessageCommand[item.EventType]);
                        break;
                    default:
                        throw new Exception($"Invalid message type: {parsedMessage.MessageType}");
                }
            }
            catch
            {
                CommonMessage errorMessage;
                if (item.EventType == "MOR")
                {
                    errorMessage = new ExtractionErrorMessage((BaseMessage) parsedMessage);
                }
                else if (item.EventType == "NAT")
                {
                    errorMessage = new BirthRecordErrorMessage((BFDRBaseMessage) parsedMessage);
                }
                else if (item.EventType == "FET")
                {
                    errorMessage = new FetalDeathRecordErrorMessage((BFDRBaseMessage) parsedMessage);
                }
                else
                {
                    throw new Exception($"Invalid Event Type: {item.EventType}");
                }
                outgoingMessageItem.Message = errorMessage.ToJSON();
                outgoingMessageItem.MessageId = errorMessage.MessageId;
                outgoingMessageItem.MessageType = errorMessage.GetType().Name;
                outgoingMessageItem.CertificateNumber = errorMessage.CertNo.ToString().PadLeft(6, '0');
                outgoingMessageItem.EventYear = errorMessage.GetYear();
                outgoingMessageItem.EventType = item.EventType;
                this._context.OutgoingMessageItems.Add(outgoingMessageItem);
            }
            await this._context.SaveChangesAsync();
        }

        private void HandleSubmissionMessage(CommonMessage message, IncomingMessageItem databaseMessage, Func<CommonMessage, string> createIJEString, Action<CommonMessage, IncomingMessageItem, ApplicationDbContext> createAckMessage)
        {
            IJEItem ijeItem = new IJEItem();
            ijeItem.MessageId = message.MessageId;
            // set validation to false
            ijeItem.IJE = createIJEString(message);
            // Log and ack message right after it is successfully extracted
            createAckMessage(message, databaseMessage, this._context);
            bool duplicateMessage = IncomingMessageLogItemExists(message.MessageId);
            // Log the message whether or not it is a duplicate
            LogMessage(message, databaseMessage);
            // Only save non-duplicate submission messages
            if(!duplicateMessage) {
                this._context.IJEItems.Add(ijeItem);
                this._context.SaveChanges();
            }
        }

        private void HandleUpdateMessage(CommonMessage message, IncomingMessageItem databaseMessage, Func<CommonMessage, string> createIJEString, Action<CommonMessage, IncomingMessageItem, ApplicationDbContext> createAckMessage)
        {
            IJEItem ijeItem = new IJEItem();
            ijeItem.MessageId = message.MessageId;
            ijeItem.IJE = createIJEString(message);
            createAckMessage(message, databaseMessage, this._context);
            bool duplicateMessage = IncomingMessageLogItemExists(message.MessageId);
            IncomingMessageLog previousMessage = LatestMessageByNCHSId(message.NCHSIdentifier);
            if(!duplicateMessage) {
                // Only log messages that are not duplicates
                LogMessage(message, databaseMessage);
                // Only save if this is not a message with a duplicate ID and the previousMessage either does not exist or
                // has an older timestamp than the message we are currently dealing with
                if(previousMessage == null || message.MessageTimestamp > previousMessage.MessageTimestamp) {
                    this._context.IJEItems.Add(ijeItem);
                    this._context.SaveChanges();
                }
            }
        }

        private static void CreateAckMessage<T>(CommonMessage message, IncomingMessageItem databaseMessage, string eventType, Func<CommonMessage, T> createMessage, ApplicationDbContext _context) where T: CommonMessage
        {
            OutgoingMessageItem outgoingMessageItem = new OutgoingMessageItem();
            CommonMessage ackMessage = createMessage.Invoke(message);
            outgoingMessageItem.JurisdictionId = databaseMessage.JurisdictionId;
            outgoingMessageItem.Message = ackMessage.ToJSON();
            outgoingMessageItem.MessageId = ackMessage.MessageId;
            outgoingMessageItem.MessageType = ackMessage.GetType().Name;
            outgoingMessageItem.CertificateNumber = ackMessage.CertNo.ToString().PadLeft(6, '0');
            outgoingMessageItem.EventYear = ackMessage.GetYear();
            outgoingMessageItem.EventType = eventType;
            outgoingMessageItem.IGVersion = databaseMessage.IGVersion;
            _context.OutgoingMessageItems.Add(outgoingMessageItem);
            _context.SaveChanges();
        }

        private void LogMessage(CommonMessage message, IncomingMessageItem databaseMessage) {
            IncomingMessageLog entry = new IncomingMessageLog();
            entry.MessageTimestamp = message.MessageTimestamp;
            entry.MessageId = message.MessageId;
            entry.JurisdictionId = databaseMessage.JurisdictionId;
            entry.NCHSIdentifier = message.NCHSIdentifier;
            entry.StateAuxiliaryIdentifier = message.StateAuxiliaryId;
            this._context.IncomingMessageLogs.Add(entry);
            this._context.SaveChanges();
        }

        private bool IncomingMessageLogItemExists(string messageId)
        {
            return this._context.IncomingMessageLogs.Any(l => l.MessageId == messageId);
        }

        private IncomingMessageLog LatestMessageByNCHSId(string NCHSIdentifier)
        {
            return this._context.IncomingMessageLogs.Where(l => l.NCHSIdentifier == NCHSIdentifier).OrderBy(l => l.MessageTimestamp).LastOrDefault();
        }

    }
  }
}
