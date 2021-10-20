using messaging.Models;
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
            BaseMessage parsedMessage = BaseMessage.Parse(item.Message.ToString(), true);
            IJEItem ijeItem = new IJEItem();
            OutgoingMessageItem outgoingMessageItem = new OutgoingMessageItem();
            try {
                switch(parsedMessage) {
                    case DeathRecordUpdate update:
                        HandleUpdateMessage(update);
                    break;
                    case DeathRecordSubmission submission:
                        HandleSubmissionMessage(submission);
                    break;
                }
            } catch {
                ExtractionErrorMessage errorMessage = new ExtractionErrorMessage(parsedMessage);
                outgoingMessageItem.Message = errorMessage.ToJSON();
                this._context.OutgoingMessageItems.Add(outgoingMessageItem);
            }
            await this._context.SaveChangesAsync();
          }

        private void HandleSubmissionMessage(DeathRecordSubmission message) {
            IJEItem ijeItem = new IJEItem();
            ijeItem.MessageId = message.MessageId;
            ijeItem.IJE = new IJEMortality(message.DeathRecord).ToString();
            // Log and ack message right after it is successfully extracted
            CreateAckMessage(message);
            bool duplicateMessage = IncomingMessageLogItemExists(message.MessageId);
            // Log the message whether or not it is a duplicate
            LogMessage(message);
            // Only save non-duplicate submission messages
            if(!duplicateMessage) {
                this._context.IJEItems.Add(ijeItem);
                this._context.SaveChanges();
            }
        }

        private void HandleUpdateMessage(DeathRecordUpdate message) {
            IJEItem ijeItem = new IJEItem();
            ijeItem.MessageId = message.MessageId;
            ijeItem.IJE = new IJEMortality(message.DeathRecord).ToString();
            CreateAckMessage(message);
            bool duplicateMessage = IncomingMessageLogItemExists(message.MessageId);
            IncomingMessageLog previousMessage = LatestMessageByNCHSAndStateId(message.NCHSIdentifier, message.StateAuxiliaryIdentifier);
            if(!duplicateMessage) {
                // Only log messages that are not duplicates
                LogMessage(message);
                // Only save if this is not a message with a duplicate ID and the previousMessage either does not exist or
                // has an older timestamp than the message we are currently dealing with
                if(previousMessage == null || message.MessageTimestamp > previousMessage.MessageTimestamp) {
                    this._context.IJEItems.Add(ijeItem);
                    this._context.SaveChanges();
                }
            }
        }

        private void LogMessage(DeathRecordSubmission message) {
            IncomingMessageLog entry = new IncomingMessageLog();
            entry.MessageTimestamp = message.MessageTimestamp;
            entry.MessageId = message.MessageId;
            entry.NCHSIdentifier = message.NCHSIdentifier;
            entry.StateAuxiliaryIdentifier = message.StateAuxiliaryIdentifier;
            this._context.IncomingMessageLogs.Add(entry);
            this._context.SaveChanges();
        }

        private void CreateAckMessage(BaseMessage message) {
            OutgoingMessageItem outgoingMessageItem = new OutgoingMessageItem();
            AckMessage ackMessage = new AckMessage(message);
            outgoingMessageItem.Message = ackMessage.ToJSON();
            outgoingMessageItem.MessageId = ackMessage.MessageId;
            this._context.OutgoingMessageItems.Add(outgoingMessageItem);
            this._context.SaveChanges();
        }

        private bool IncomingMessageLogItemExists(string messageId)
        {
            return this._context.IncomingMessageLogs.Any(l => l.MessageId == messageId);
        }

        private IncomingMessageLog LatestMessageByNCHSAndStateId(string NCHSIdentifier, string stateId)
        {
            return this._context.IncomingMessageLogs.Where(l => l.NCHSIdentifier == NCHSIdentifier && l.StateAuxiliaryIdentifier == stateId).OrderBy(l => l.MessageTimestamp).LastOrDefault();
        }

        private bool IncomingMessageItemExists(long id)
        {
            return this._context.IncomingMessageItems.Any(e => e.Id == id);
        }
    }
  }
}
