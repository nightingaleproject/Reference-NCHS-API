using messaging.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VRDR;

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

          public async Task DoWork(Message message, CancellationToken cancellationToken)
        {
            IncomingMessageItem item = this._context.IncomingMessageItems.Find(message.Id);
            item.ProcessedStatus = "PROCESSED";
            this._context.Update(item);
            BaseMessage parsedMessage = BaseMessage.Parse(item.Message.ToString(), true);
            OutgoingMessageItem outgoingMessageItem = new OutgoingMessageItem();
            outgoingMessageItem.JurisdictionId = item.JurisdictionId;
            try {
                switch(parsedMessage) {
                    case DeathRecordUpdateMessage update:
                        HandleUpdateMessage(update, item);
                    break;
                    case DeathRecordSubmissionMessage submission:
                        HandleSubmissionMessage(submission, item);
                    break;
                }
            } catch {
                ExtractionErrorMessage errorMessage = new ExtractionErrorMessage(parsedMessage);
                outgoingMessageItem.Message = errorMessage.ToJSON();
                outgoingMessageItem.MessageId = errorMessage.MessageId;
                outgoingMessageItem.MessageType = errorMessage.GetType().Name;
                this._context.OutgoingMessageItems.Add(outgoingMessageItem);
            }
            await this._context.SaveChangesAsync();
          }

        private void HandleSubmissionMessage(DeathRecordSubmissionMessage message, IncomingMessageItem databaseMessage) {
            IJEItem ijeItem = new IJEItem();
            ijeItem.MessageId = message.MessageId;
            // set validation to false
            ijeItem.IJE = new IJEMortality(message.DeathRecord, false).ToString();
            // Log and ack message right after it is successfully extracted
            CreateAckMessage(message, databaseMessage);
            bool duplicateMessage = IncomingMessageLogItemExists(message.MessageId);
            // Log the message whether or not it is a duplicate
            LogMessage(message, databaseMessage);
            // Only save non-duplicate submission messages
            if(!duplicateMessage) {
                this._context.IJEItems.Add(ijeItem);
                this._context.SaveChanges();
            }
        }

        private void HandleUpdateMessage(DeathRecordUpdateMessage message, IncomingMessageItem databaseMessage) {
            IJEItem ijeItem = new IJEItem();
            ijeItem.MessageId = message.MessageId;
            ijeItem.IJE = new IJEMortality(message.DeathRecord, false).ToString();
            CreateAckMessage(message, databaseMessage);
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

        private void LogMessage(DeathRecordSubmissionMessage message, IncomingMessageItem databaseMessage) {
            IncomingMessageLog entry = new IncomingMessageLog();
            entry.MessageTimestamp = message.MessageTimestamp;
            entry.MessageId = message.MessageId;
            entry.JurisdictionId = databaseMessage.JurisdictionId;
            entry.NCHSIdentifier = message.NCHSIdentifier;
            entry.StateAuxiliaryIdentifier = message.StateAuxiliaryId;
            this._context.IncomingMessageLogs.Add(entry);
            this._context.SaveChanges();
        }

        private void CreateAckMessage(BaseMessage message, IncomingMessageItem databaseMessage) {
            OutgoingMessageItem outgoingMessageItem = new OutgoingMessageItem();
            AcknowledgementMessage ackMessage = new AcknowledgementMessage(message);
            outgoingMessageItem.JurisdictionId = databaseMessage.JurisdictionId;
            outgoingMessageItem.Message = ackMessage.ToJSON();
            outgoingMessageItem.MessageId = ackMessage.MessageId;
            outgoingMessageItem.MessageType = ackMessage.GetType().Name;
            this._context.OutgoingMessageItems.Add(outgoingMessageItem);
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
