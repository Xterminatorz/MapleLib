using System;
using System.Net.Sockets;
using MapleLib.MapleCryptoLib;

namespace MapleLib.PacketLib {
	/// <summary>
	/// Class to a network session socket
	/// </summary>
	public class Session {
		/// <summary>
		/// The Session's socket
		/// </summary>
		private readonly Socket mSocket;

		private readonly SessionType mType;

		/// <summary>
		/// The Recieved packet crypto manager
		/// </summary>
		private MapleCrypto mRIV;

		/// <summary>
		/// The Sent packet crypto manager
		/// </summary>
		private MapleCrypto mSIV;

		/// <summary>
		/// Method to handle packets received
		/// </summary>
		public delegate void PacketReceivedHandler(PacketReader pPacket, bool pIsInit);

		/// <summary>
		/// Packet received event
		/// </summary>
		public event PacketReceivedHandler OnPacketReceived;

		/// <summary>
		/// Method to handle client disconnected
		/// </summary>
		public delegate void ClientDisconnectedHandler(Session pSession);

		/// <summary>
		/// Client disconnected event
		/// </summary>
		public event ClientDisconnectedHandler OnClientDisconnected;

		public delegate void InitPacketReceived(short pVersion, byte pServerIdentifier);

		public event InitPacketReceived OnInitPacketReceived;

		/// <summary>
		/// The Recieved packet crypto manager
		/// </summary>
		public MapleCrypto RIV { get { return mRIV; } set { mRIV = value; } }

		/// <summary>
		/// The Sent packet crypto manager
		/// </summary>
		public MapleCrypto SIV { get { return mSIV; } set { mSIV = value; } }

		/// <summary>
		/// The Session's socket
		/// </summary>
		public Socket Socket { get { return mSocket; } }

		public SessionType Type { get { return mType; } }

		/// <summary>
		/// Creates a new instance of a Session
		/// </summary>
		/// <param name="pSocket">Socket connection of the session</param>
		public Session(Socket pSocket, SessionType pType) {
			mSocket = pSocket;
			mType = pType;
		}

		/// <summary>
		/// Waits for more data to arrive
		/// </summary>
		public void WaitForData() {
			WaitForData(new SocketInfo(mSocket, 4));
		}

		public void WaitForDataNoEncryption() {
			WaitForData(new SocketInfo(mSocket, 2, true));
		}

		/// <summary>
		/// Waits for more data to arrive
		/// </summary>
		/// <param name="pSocketInfo">Info about data to be received</param>
		private void WaitForData(SocketInfo pSocketInfo) {
			try {
				mSocket.BeginReceive(pSocketInfo.DataBuffer, pSocketInfo.Index, pSocketInfo.DataBuffer.Length - pSocketInfo.Index, SocketFlags.None, new AsyncCallback(OnDataReceived), pSocketInfo);
			} catch (Exception se) {
				Console.WriteLine("[Error] Session.WaitForData: " + se);
			}
		}

		/// <summary>
		/// Data received event handler
		/// </summary>
		/// <param name="pIAR">IAsyncResult of the data received event</param>
		private void OnDataReceived(IAsyncResult pIAR) {
			SocketInfo socketInfo = (SocketInfo) pIAR.AsyncState;
			try {
				int received = socketInfo.Socket.EndReceive(pIAR);
				if (received == 0) {
					if (OnClientDisconnected != null) {
						OnClientDisconnected(this);
					}
					return;
				}

				socketInfo.Index += received;

				if (socketInfo.Index == socketInfo.DataBuffer.Length) {
					switch (socketInfo.State) {
						case SocketInfo.StateEnum.Header:
							if (socketInfo.NoEncryption) {
								PacketReader headerReader = new PacketReader(socketInfo.DataBuffer);
								short packetHeader = headerReader.ReadShort();
								socketInfo.State = SocketInfo.StateEnum.Content;
								socketInfo.DataBuffer = new byte[packetHeader];
								socketInfo.Index = 0;
								WaitForData(socketInfo);
							} else {
								PacketReader headerReader = new PacketReader(socketInfo.DataBuffer);
								byte[] packetHeaderB = headerReader.ToArray();
								int packetHeader = headerReader.ReadInt();
								short packetLength = (short) MapleCrypto.getPacketLength(packetHeader);
								if (mType == SessionType.SERVER_TO_CLIENT && !mRIV.checkPacketToServer(BitConverter.GetBytes(packetHeader))) {
									Console.WriteLine("[Error] Packet check failed. Disconnecting client.");
									//this.Socket.Close();
								}
								socketInfo.State = SocketInfo.StateEnum.Content;
								socketInfo.DataBuffer = new byte[packetLength];
								socketInfo.Index = 0;
								WaitForData(socketInfo);
							}
							break;
						case SocketInfo.StateEnum.Content:
							byte[] data = socketInfo.DataBuffer;
							if (socketInfo.NoEncryption) {
								socketInfo.NoEncryption = false;
								PacketReader reader = new PacketReader(data);
								short version = reader.ReadShort();
								string unknown = reader.ReadMapleString();
								mSIV = new MapleCrypto(reader.ReadBytes(4), version);
								mRIV = new MapleCrypto(reader.ReadBytes(4), version);
								byte serverType = reader.ReadByte();
								if (mType == SessionType.CLIENT_TO_SERVER) {
									OnInitPacketReceived(version, serverType);
								}
								OnPacketReceived(new PacketReader(data), true);
								WaitForData();
							} else {
								mRIV.crypt(data);
								MapleCustomEncryption.Decrypt(data);
								if (data.Length != 0 && OnPacketReceived != null) {
									OnPacketReceived(new PacketReader(data), false);
								}
								WaitForData();
							}
							break;
					}
				} else {
					Console.WriteLine("[Warning] Not enough data");
					WaitForData(socketInfo);
				}
			} catch (ObjectDisposedException) {
				Console.WriteLine("[Error] Session.OnDataReceived: Socket has been closed");
			} catch (SocketException se) {
				if (se.ErrorCode != 10054) {
					Console.WriteLine("[Error] Session.OnDataReceived: " + se);
				}
			} catch (Exception e) {
				Console.WriteLine("[Error] Session.OnDataReceived: " + e);
			}
		}

		public void SendInitialPacket(int pVersion, string pPatchLoc, byte[] pRIV, byte[] pSIV, byte pServerType) {
			PacketWriter writer = new PacketWriter();
			writer.WriteShort(string.IsNullOrEmpty(pPatchLoc) ? 0x0D : 0x0E);
			writer.WriteShort(pVersion);
			writer.WriteMapleString(pPatchLoc);
			writer.WriteBytes(pRIV);
			writer.WriteBytes(pSIV);
			writer.WriteByte(pServerType);
			SendRawPacket(writer);
		}

		/// <summary>
		/// Encrypts the packet then send it to the client.
		/// </summary>
		/// <param name="pPacket">The PacketWrtier object to be sent.</param>
		public void SendPacket(PacketWriter pPacket) {
			SendPacket(pPacket.ToArray());
		}

		/// <summary>
		/// Encrypts the packet then send it to the client.
		/// </summary>
		/// <param name="pInput">The byte array to be sent.</param>
		public void SendPacket(byte[] pInput) {
			byte[] cryptData = pInput;
			byte[] sendData = new byte[cryptData.Length + 4];
			byte[] header = mType == SessionType.SERVER_TO_CLIENT ? mSIV.getHeaderToClient(cryptData.Length) : mSIV.getHeaderToServer(cryptData.Length);

			MapleCustomEncryption.Encrypt(cryptData);
			mSIV.crypt(cryptData);

			Buffer.BlockCopy(header, 0, sendData, 0, 4);
			Buffer.BlockCopy(cryptData, 0, sendData, 4, cryptData.Length);
			SendRawPacket(sendData);
		}

		/// <summary>
		/// Sends a raw packet to the client
		/// </summary>
		/// <param name="pPacket">The PacketWriter</param>
		public void SendRawPacket(PacketWriter pPacket) {
			SendRawPacket(pPacket.ToArray());
		}

		/// <summary>
		/// Sends a raw buffer to the client.
		/// </summary>
		/// <param name="pBuffer">The buffer to be sent.</param>
		public void SendRawPacket(byte[] pBuffer) {
			//_socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, ar => _socket.EndSend(ar), null);//async
			mSocket.Send(pBuffer); //sync
		}
	}
}