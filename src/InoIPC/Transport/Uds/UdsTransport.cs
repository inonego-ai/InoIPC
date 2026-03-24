using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// Unix Domain Socket transport. Raw byte stream over UDS.
   /// </summary>
   // ============================================================
   public class UdsTransport : ITransport
   {

   #region Fields

      public bool IsConnected => socket != null && socket.Connected;

      private readonly string socketPath;

      private Socket socket;

   #endregion

   #region Constructors

      // ------------------------------------------------------------
      /// <summary>
      /// Creates a transport that will connect to a socket path.
      /// </summary>
      // ------------------------------------------------------------
      public UdsTransport(string socketPath)
      {
         this.socketPath = socketPath;
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Wraps an already-connected socket (e.g. from server).
      /// <br/> Connect() becomes a no-op.
      /// </summary>
      // ----------------------------------------------------------------------
      public UdsTransport(Socket acceptedSocket)
      {
         this.socketPath = null;
         this.socket     = acceptedSocket;
      }

   #endregion

   #region ITransport

      // ------------------------------------------------------------
      /// <summary>
      /// Connects to the Unix Domain Socket at the configured path.
      /// </summary>
      // ------------------------------------------------------------
      public void Connect()
      {
         var endpoint = new UnixDomainSocketEndPoint(socketPath);

         socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
         socket.Connect(endpoint);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Writes raw bytes to the socket.
      /// </summary>
      // ------------------------------------------------------------
      public void Write(byte[] data, int offset, int count)
      {
         socket.Send(data, offset, count, SocketFlags.None);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Reads raw bytes from the socket.
      /// </summary>
      // ------------------------------------------------------------
      public int Read(byte[] buffer, int offset, int count)
      {
         return socket.Receive(buffer, offset, count, SocketFlags.None);
      }

   #endregion

   #region IDisposable

      public void Dispose()
      {
         socket?.Close();
         socket?.Dispose();
      }

   #endregion

   }
}
