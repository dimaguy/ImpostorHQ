namespace ImpostorHQ.Core.Config
{
    public class PrimaryConfig
    {
        public const string Section = "IHQHttp";

        public string Host { get; set; } = "0.0.0.0";

        public string DashboardEndPoint { get; set; } = "/ihq";

        public ushort HttpPort { get; set; } = 80;

        public ushort ApiPort { get; set; } = 22023;
    }
}