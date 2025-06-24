# Mark messages in the queue as processed at a steady rate; does not actually do any processing
# Requires freetds to be installed in addition to tiny_tds gem

require 'tiny_tds'
require 'json'

# Takes one argument: messages per minute (defaults to 10)
messages_per_minute = (ARGV.shift || 10).to_i

# Load DB information from appsettings
settings = JSON.parse(File.read(File.join(__dir__, '..', 'messaging', 'appsettings.Development.json')))
connection_string = settings['ConnectionStrings']['NVSSMessagingDatabase']
credentials = connection_string.split(';').map { |f| f.split('=') }.to_h

# Set up the DB connection
client = TinyTds::Client.new(username: credentials['User'],
                             password: credentials['Password'],
                             host: credentials['Server'],
                             port: 1433,
                             database: credentials['Database'],
                             azure: false)

# Query just takes the first QUEUED message and marks it as PROCESSED
query = "UPDATE IncomingMessageItems SET ProcessedStatus='PROCESSED', UpdatedDate=CURRENT_TIMESTAMP " \
        "WHERE Id=(SELECT TOP 1 Id FROM IncomingMessageItems WHERE ProcessedStatus='QUEUED')"

while true do
  client.execute(query)
  if client.execute('SELECT @@ROWCOUNT AS count').first['count'] > 0
    puts "Processed a message"
  else
    puts "No messages to process"
  end
  sleep 60 / messages_per_minute.to_f
end
