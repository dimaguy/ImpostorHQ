using System;
using BenchmarkDotNet.Running;

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