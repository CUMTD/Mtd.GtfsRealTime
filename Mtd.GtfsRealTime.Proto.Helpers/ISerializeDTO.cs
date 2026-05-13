using Google.Protobuf;

namespace Mtd.GtfsRealTime.Proto.Helpers;

public interface ISerializeDTO : IMessage
{
	public byte[] Serialize() => this.ToByteArray();
}
