# WebApi dependency.

This class exports a thread-safe collection of the actual authenticated users. It is obtained from the underlying ConcurrentDictionary. Access it by the `Users` property.

It also exports a function called `BroadcastAsync`, which just enumerates trough the users and awaits sending the message supplied in the parameters. It returns the number of API clients the message has been sent to.

