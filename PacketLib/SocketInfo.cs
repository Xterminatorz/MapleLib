using System.Net.Sockets;

namespace MapleLib.PacketLib {
	/// <summary>
	/// Class to manage Socket and data to receive
	/// </summary>
	public class SocketInfo {
		/// <summary>
		/// Creates a new instance of a SocketInfo
		/// </summary>
		/// <param name="pSocket">Socket connection of the session</param>
		/// <param name="pHeaderLength">Length of the main packet's header (Usually 4)</param>
		public SocketInfo(Socket pSocket, short pHeaderLength, bool pNoEncryption = false) {
			Socket = pSocket;
			State = StateEnum.Header;
			NoEncryption = pNoEncryption;
			DataBuffer = new byte[pHeaderLength];
			Index = 0;
		}

		/// <summary>
		/// The SocketInfo's socket
		/// </summary>
		public readonly Socket Socket;

		public bool NoEncryption;

		/// <summary>
		/// The Session's state of what data to receive
		/// </summary>
		public StateEnum State;

		/// <summary>
		/// The buffer of data to recieve
		/// </summary>
		public byte[] DataBuffer;

		/// <summary>
		/// The index of the current data
		/// </summary>
		public int Index;

		/// <summary>
		/// The SocketInfo's state of data
		/// </summary>
		public enum StateEnum {
			Header,
			Content
		}
	}
}