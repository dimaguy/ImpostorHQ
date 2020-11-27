## ImpostorHQ Plugin System - The HTTP Server

In this lesson, we will be getting up close and personal with `antiHttp(s)`. We will cover the general concepts associated with it, and by the end of this lesson, you will be able to hook your own HTTP web handlers.

## 1.What is antiHTTP?

AntiHTTP is the workhorse of the web front end. It serves the HTML pages directly from the plugin. It is specifically written for this task, so it is highly performant, compared to regular(general purpose) servers.

The server can handle local files (found in the `dashboard` directory), custom handlers (`e.g the player list API`), and supports the following MIME types (more can be very easily added):

```
.html
.htm
.js
.css
.ico
.jpeg
.jpg
.png
.gif
.ttf
```



### The local file handler.

​	Pages have content. That content is stored locally, on the HTTP server's root. In order to write your own complex pages, you must put your files in `/dashboard`. They immediately become accessible trough the HTTP path.

​	The local file handler does not support indexing. This prevents attackers from probing the file system. The robustness also increases the performance of the handler.

### The special handler.

​	Sometimes, the server needs to serve other kinds of information. A prime example of that is the player list API. The server handles a query request directly from the URL path. This implementation allows APIs to be easily created from within the plugin.

Special handlers can only be created for files that do not exist on the HTTP server's root (you cannot create a handler that targets E.G `/client.html` ).

## 2. Security.

AntiHTTP supports HTTP and HTTPS over TLS. The first time the plugin is ran, it creates 2 certificates that are used by the server and clients. The clients must register the self-signed certificate in their browser in order to connect to the server securely.

The security option is found in the main plugin's config file. (`configs/Impostor.Command.Core.cfg`). It is called `UseSsl`, and it is disabled, by default.

The transport protocol uses TLS 1.2, which is widely supported by browsers. The same certificate is also used to encrypt the Web Sockets API server.

## 3. Using antiHTTP in your plugins.

###  Prerequisites 

Accessing the HTTP server is done from the plugin's state object (`QuiteExtendableDirectInterface`), which is injected at start-up.

In order to access the instance, you can use the `QuiteExtendableDirectInterface.DashboardServer` property. From now on, I will be referring to this property as `server`.

### Creating a special page handler

Creating a special page handler is easy. Just call the following function, which will register the handler dynamically:

```c#
server.AddInvalidPageHandler("myHandler");
```

The handler is now registered and ready to use.

### Hooking a special page handler.

Hooking a special page handler is just as easy as creating it. The process is very similar to hooking commands. It can be done as follows:

```c#
server.OnSpecialHandlerInvoked += MySpecialHandler;
```

Beware that the event is called for any special handler. You must properly check to see if it is your handler that you are working with. To do that, you can use a simple switch statement, or if-else loops. 

The following is a full example of how to create a special handler and how to use it:

```C#
const string myHandler = "hello";
var server = reference.DashboardServer;
server.AddInvalidPageHandler(myHandler);
server.OnSpecialHandlerInvoked += (handler, transport, httpVersion, ipAddress) =>
{
    switch (handler)
    {
        case myHandler:
        {
            var response = Encoding.UTF8.GetBytes($"<h1> Hi there! It is {DateTime.Now} at the moment.</h1>");
            server.WriteDocument(response,"text/html",transport);
            break;
        }
    }
};
```

![https://femto.pw/f7ht](https://femto.pw/f7ht)

That's about it! You can now write your own HTTP web handlers.

See you next time!

`-anti`