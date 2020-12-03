using System;

namespace Impostor.Commands.Core.Client
{
    [Serializable]
    public class BaseMessage
    {
        /// <summary>
        /// The message data.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// The type of the message. JS, please no!!!
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The UNIX date epoch.
        /// </summary>
        public ulong Date { get; set; }
        /// <summary>
        /// The source of the message.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Additional data for some messages (e.g the heartbeat)
        /// </summary>
        public float[] Flags { get; set; }
    }
}