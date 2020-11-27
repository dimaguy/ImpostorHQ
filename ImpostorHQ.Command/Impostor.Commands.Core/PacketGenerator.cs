using System.IO;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Messages;

namespace Impostor.Commands.Core
{
    public partial class Structures
    {
        public class PacketGenerator
        {
            private IMessageWriterProvider Provider { get; set; }
            public PacketGenerator(IMessageWriterProvider provider)
            {
                this.Provider = provider;
            }
            /// <summary>
            /// This is used to write an efficient chat packet.
            /// </summary>
            /// <param name="game">The Game's ID.</param>
            /// <param name="netId">The NetId of the player (as found in the Character.NetId property).</param>
            /// <param name="original">The initial name of the player.</param>
            /// <param name="chat">The chat messages.</param>
            /// <param name="source">The source name of the message. Can be any string, that does not contain special characters.</param>
            /// <returns>A packet that can be sent to the client directly.</returns>
            public IMessageWriter WriteChat(int game, uint netId, string original, string[] chat,string source)
            {
                var messageWriter = Provider.Get(MessageType.Reliable);
                messageWriter.StartMessage(Api.Net.Messages.MessageFlags.GameData);
                messageWriter.Write(game);
                messageWriter.StartMessage((byte)GameDataType.RpcFlag);
                messageWriter.WritePacked(netId);
                messageWriter.Write((byte)Structures.RpcCalls.SetName);
                messageWriter.Write(source);
                messageWriter.EndMessage();
                foreach (var message in chat)
                {
                    messageWriter.StartMessage((byte)GameDataType.RpcFlag);
                    messageWriter.WritePacked(netId);
                    messageWriter.Write((byte)Structures.RpcCalls.SendChat);
                    messageWriter.Write(message);
                    messageWriter.EndMessage();
                }
                messageWriter.StartMessage((byte)GameDataType.RpcFlag);
                messageWriter.WritePacked(netId);
                messageWriter.Write((byte)Structures.RpcCalls.SetName);
                messageWriter.Write(original);
                messageWriter.EndMessage();
                messageWriter.EndMessage();
                return messageWriter;
            }
            /// <summary>
            /// This is used to generate individual GameOptionsData packets.
            /// </summary>
            /// <param name="data">The options to serialize.</param>
            /// <param name="game">The Game's ID.</param>
            /// <param name="netId">The NetId of the player (as found in the Character.NetId property).</param>
            /// <returns>A packet that can be sent to the client directly.</returns>
            public IMessageWriter GenerateDataPacket(GameOptionsData data, int game, uint netId)
            {
                var writer = Provider.Get(MessageType.Reliable);
                writer.StartMessage(Api.Net.Messages.MessageFlags.GameData);
                writer.Write(game);
                writer.StartMessage((byte)GameDataType.RpcFlag);
                writer.WritePacked(netId);
                writer.Write((byte)Structures.RpcCalls.SyncSettings);
                using(var stream = new MemoryStream())
                using (BinaryWriter bWriter = new BinaryWriter(stream))
                {
                    data.Serialize(bWriter,4);
                    writer.WriteBytesAndSize(stream.ToArray());
                }
                writer.EndMessage();
                writer.EndMessage();
                return writer;
            }
            /// <summary>
            /// This is used to generate a name change packet.
            /// </summary>
            /// <param name="name">The new name.</param>
            /// <param name="netId">The NetId of the player (as found in the Character.NetId property).</param>
            /// <param name="gameCode">The Game's ID.</param>
            /// <returns>A packet that can be sent to the client directly.</returns>
            public IMessageWriter GenerateNameChangePacket(string name, uint netId, int gameCode)
            {
                var messageWriter = Provider.Get(MessageType.Reliable);
                messageWriter.StartMessage(Api.Net.Messages.MessageFlags.GameData);
                messageWriter.Write(gameCode);
                messageWriter.StartMessage((byte)GameDataType.RpcFlag);
                messageWriter.WritePacked(netId);
                messageWriter.Write((byte)Structures.RpcCalls.SetName);
                messageWriter.Write(name);
                messageWriter.EndMessage();
                messageWriter.EndMessage();
                return messageWriter;
            }
            private enum GameDataType : byte
            {
                RpcFlag = 2,
                SpawnFlag = 4,
                DespawnFlag = 5,
                SceneChangeFlag = 6,
                ReadyFlag = 7,
                ChangeSettingsFlag = 8
            }
        }
    }
}