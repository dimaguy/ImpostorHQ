#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ImpostorHQ.Http
{
    public static class Extensions
    {
        public static async ValueTask<string?> ReadLineSized(this Stream stream, int maxLength, CancellationToken ct = default)
        {
            await using var ms = new MemoryStream();
            var total = 0;
            var buffer = new byte[maxLength];

            while (total < maxLength)
            {
                int read;
                try
                { 
                    read = await stream.ReadAsync(buffer, ct);
                }
                catch (IOException)
                {
                    //connection closed
                    return null;
                }

                if (read == 0)
                {
                    if (total > 0) break;
                    return null;
                }

                total += read;
                ms.Write(buffer.AsSpan(0, read));

                var end = buffer[read - 1];
                if (end == 13 || end == 10) break;
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static void Write(this Stream stream, string text)
        {
            stream.Write(Encoding.UTF8.GetBytes(text));
        }

        public static IServiceCollection AddHttpServer(this IServiceCollection collection, string host, ushort port, string notFound404Html)
        {
            var server = new HttpServer(port, IPAddress.Parse(host), notFound404Html);
            collection.AddSingleton(server);
            return collection;
        }
    }
}
