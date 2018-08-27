using System.IO;

namespace MapleLib.PacketLib {
	public abstract class AbstractPacket {
		protected MemoryStream mBuffer;

		public byte[] ToArray() {
			return mBuffer.ToArray();
		}
	}
}