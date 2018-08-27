using System;
using System.Net;
using System.Net.Sockets;

namespace MapleLib.PacketLib {
	/// <summary>
	/// A Nework Socket Acceptor (Listener)
	/// </summary>
	public class Acceptor {
		/// <summary>
		/// The listener socket
		/// </summary>
		private readonly Socket mListener;

		/// <summary>
		/// Method called when a client is connected
		/// </summary>
		public delegate void ClientConnectedHandler(Session pSession);

		/// <summary>
		/// Client connected event
		/// </summary>
		public event ClientConnectedHandler OnClientConnected;

		/// <summary>
		/// Creates a new instance of Acceptor
		/// </summary>
		public Acceptor() {
			mListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		/// <summary>
		/// Starts listening and accepting connections
		/// </summary>
		/// <param name="pPort">Port to listen to</param>
		public void StartListening(int pPort) {
			mListener.Bind(new IPEndPoint(IPAddress.Any, pPort));
			mListener.Listen(15);
			mListener.BeginAccept(new AsyncCallback(OnClientConnect), null);
		}

		/// <summary>
		/// Stops listening for connections
		/// </summary>
		public void StopListening() {
			mListener.Disconnect(true);
		}

		/// <summary>
		/// Client connected handler
		/// </summary>
		/// <param name="iarl">The IAsyncResult</param>
		private void OnClientConnect(IAsyncResult pIAR) {
			try {
				Socket socket = mListener.EndAccept(pIAR);
				Session session = new Session(socket, SessionType.SERVER_TO_CLIENT);

				if (OnClientConnected != null)
					OnClientConnected(session);

				session.WaitForData();

				mListener.BeginAccept(new AsyncCallback(OnClientConnect), null);
			} catch (ObjectDisposedException) {
				Console.WriteLine("[Error] OnClientConnect: Socket closed.");
			} catch (Exception se) {
				Console.WriteLine("[Error] OnClientConnect: " + se);
			}
		}
	}
}