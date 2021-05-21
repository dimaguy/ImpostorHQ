using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using ImpostorHQ.Core.Cryptography.BlackTea;
using Microsoft.Extensions.Logging;

namespace ImpostorHQ.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Benchmarking BlackTea...";
            BenchmarkRunner.Run<TeaBenchy>();
            Console.WriteLine("Press [enter] to benchmark the ban database.");
            Console.ReadLine();
            Console.Title = "Benchmarking ban database...";
            BenchmarkRunner.Run<BanDatabaseBenchy>();
        }
    }
}
