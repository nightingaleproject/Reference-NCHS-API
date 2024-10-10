# Submit a stream of messages to the NVSS API from various jurisdictions over time

# Takes two arguments: messages per minute (defaults to 10) and number of jurisdictions (also defaults to 10)

messages_per_minute = (ARGV.shift || 10).to_i
number_of_jurisdictions = (ARGV.shift || 10).to_i

# NEVER IN REAL LIFE!!!
#require 'openssl'
#OpenSSL::SSL::VERIFY_PEER = OpenSSL::SSL::VERIFY_NONE

require 'active_support/all'
require 'securerandom'
require 'time'
require 'json'
require 'parallel'
require 'uri'
require 'net/http'

# We just care about filling the various queues so we just use a small void message
def generate_void(jurisdiction)
  header_id = SecureRandom.uuid
  parameters_id = SecureRandom.uuid
  return {
    resourceType: "Bundle",
    id: SecureRandom.uuid,
    type: "message",
    timestamp: Time.now.iso8601,
    entry: [
      {
        fullUrl: "urn:uuid:#{header_id}",
        resource: {
          resourceType: "MessageHeader",
          id: header_id,
          eventUri: "http://nchs.cdc.gov/vrdr_submission_void",
          destination: [{ endpoint: "http://nchs.cdc.gov/vrdr_submission" }],
          source: { endpoint: "http://mitre.org/vrdr" }
        }
      },
      {
        fullUrl: "urn:uuid:#{parameters_id}",
        resource: {
          resourceType:  "Parameters",
          id: parameters_id,
          parameter: [
            { name: "cert_no", valueUnsignedInt: 1 },
            { name: "death_year", valueUnsignedInt: Date.today.year },
            { name: "jurisdiction_id", valueString: jurisdiction }
          ]
        }
      }
    ]
  }.to_json
end

def send_message(jurisdiction)
  begin
    uri = URI("https://localhost:5001/#{jurisdiction}/Bundles")
    body = generate_void(jurisdiction)
    https = Net::HTTP.new(uri.host, uri.port)
    https.use_ssl = true
    https.verify_mode = OpenSSL::SSL::VERIFY_NONE
    request = Net::HTTP::Post.new(uri.path)
    request['Content-Type'] = 'application/json'
    response = https.request(request, body)
    puts "Sent message to #{jurisdiction}"
  rescue => e
    puts "ERROR! Failed to send message: #{e.message}"
    puts e.backtrace.join("\n")
  end
end

jurisdictions = [
  "AK", "AL", "AR", "AS", "AZ", "CA", "CO", "CT", "DC", "DE", "FL", "GA", "GU", "HI", "IA", "ID", "IL", "IN", "KS",
  "KY", "LA", "MA", "MD", "ME", "MI", "MN", "MO", "MP", "MS", "MT", "NC", "ND", "NE", "NH", "NJ", "NM", "NV", "NY",
  "OH", "OK", "OR", "PA", "PR", "RI", "SC", "SD", "TN", "TX", "UT", "VA", "VI", "VT", "WA", "WI", "WV", "WY", "YC"
]

while true do
  send_message(jurisdictions[0,number_of_jurisdictions].sample)
  sleep 60 / messages_per_minute.to_f
end
