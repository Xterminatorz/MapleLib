using System.Net;
using System.Net.Sockets;

namespace MapleLib.PacketLib {
    /// <summary>
    /// Socket class to connect to a listener
    /// </summary>
    public class Connector {
        /// <summary>
        /// The connecting socket
        /// </summary>
        private readonly Socket mSocket;

        /// <summary>
        /// Method called when the client connects
        /// </summary>
        public delegate void ClientConnectedHandler(Session pSession);

        /// <summary>
        /// Client connected event
        /// </summary>
        public event ClientConnectedHandler OnClientConnected;

        /// <summary>
        /// Creates a new instance of Acceptor
        /// </summary>
        public Connector() {
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Connects to a listener
        /// </summary>
        /// <param name="pEP">IPEndPoint of listener</param>
        /// <returns>Session connecting to</returns>
        public Session Connect(IPEndPoint pEP) {
            mSocket.Connect(pEP);
            return CreateSession();
        }

        /// <summary>
        /// Connects to a listener
        /// </summary>
        /// <param name="pIP">IPAdress of listener</param>
        /// <param name="pPort">Port of listener</param>
        /// <returns>Session connecting to</returns>
        public Session Connect(IPAddress pIP, int pPort) {
            mSocket.Connect(pIP, pPort);
            return CreateSession();
        }

        /// <summary>
        /// Connects to a listener
        /// </summary>
        /// <param name="pIP">IPAdress's of listener</param>
        /// <param name="pPort">Port of listener</param>
        /// <returns>Session connecting to</returns>
        public Session Connect(IPAddress[] pIP, int pPort) {
            mSocket.Connect(pIP, pPort);
            return CreateSession();
        }

        /// <summary>
        /// Connects to a listener
        /// </summary>
        /// <param name="pIP">IPAdress of listener</param>
        /// <param name="pPort">Port of listener</param>
        /// <returns>Session connecting to</returns>
        public Session Connect(string pIP, int pPort) {
            mSocket.Connect(pIP, pPort);
            return CreateSession();
        }

        /// <summary>
        /// Creates the session after connecting
        /// </summary>
        /// <returns>Session created with listener</returns>
        private Session CreateSession() {
            Session session = new Session(mSocket, SessionType.CLIENT_TO_SERVER);

            if (OnClientConnected != null)
                OnClientConnected(session);

            session.WaitForDataNoEncryption();

            return session;
        }
    }
}