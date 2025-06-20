# API Testing Tools

This directory includes some simple tools for testing the API while doing development. There are two main tools:

## Dependencies

- [Ruby 3](https://www.ruby-lang.org/en/downloads/)

## exercise_local_api.rb

This tool sends a repeated series of simple void messages to the local API endpoint. It takes two optional arguments:

* the number of messages to try to send per minute
** defaults to 10
** messages are sent sequentially and this is rate limited by the API's responsiveness
* the number of jurisdictions to send messages from, ranging from 1-57
** defaults to 10
** sends a statistically equal number of messages from each jurisdiction

Usage:

```
ruby exercise_local_api.rb <messages-per-minute> <number-of-jurisdictions>
```

After running it for as many minutes as you want, exit the program with SIGINT (Ctrl-C).

## fake_queue_process.rb

This tool connects directly to the local development database docker instance and marks messages as having been processed, moving the `ProcessedStatus` from `QUEUED` to `PROCESSED` and updating `UpdatedDate` on a FIFO basis. Does not actually process messages, just marks them as processed.

When using this too it's recommended that the `AckAndIJEConversion` setting in `messaging/appsettings.Development.json` be set to `false`.

This tool requires the `tiny_tds` gem to be installed:

```
gem install tiny_tds
```

The tool takes one optional argument:

* the number of messages to try to mark as `PROCESSED` per minute
** defaults to 10

Usage:

```
ruby fake_queue_process.rb <messages-per-minute>
```
