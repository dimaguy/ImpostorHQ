# HttpServer dependency.

This class adds a function to add HTTP Handlers. There are 2 handler included by default, the StaticHandler and the DynamicHandler.

The StaticHandler takes a file path or a byte array. If a file path is provided, the data is read and stored. Then, when a client requests the endpoint, the file will be written along with an HTTP 200 status code, in one single async IO operation.

The DynamicHandler is similar to the StaticHandler, but it has a `Func<(string mine, byte[] data)>` argument that is called when the data is requested. After the data is obtained, it is written to a memory stream, along with a header, then the memory stream is copied onto the transport stream in one single async IO operation.