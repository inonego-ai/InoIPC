using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// TCP transport. Raw byte stream over TCP connection.
   /// </summary>
   // ============================================================
   public class TcpTransport : ITransport
   {

   #region Fields

      public bool IsConnected => client != null && client.Connected;

      private readonly string host;
      private readonly int    port;

      private TcpClient     client;
      private NetworkStream stream;

   #endregion

   #region Constructors

      // ------------------------------------------------------------
      /// <summary>
      /// Creates a transport that will connect to host:port.
      /// </summary>
      // ------------------------------------------------------------
      public TcpTransport(int port, string host = "127.0.0.1")
      {
         this.host = host;
         this.port = port;
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Wraps an already-connected TcpClient (e.g. from server).
      /// <br/> Connect() becomes a no-op.
      /// </summary>
      // ----------------------------------------------------------------------
      public TcpTransport(TcpClient acceptedClient)
      {
         this.host   = null;
         this.port   = 0;
         this.client = acceptedClient;
         this.stream = acceptedClient.GetStream();
      }

   #endregion

   #region ITransport

      // ------------------------------------------------------------
      /// <summary>
      /// Opens a TCP connection to the target host and port.
      /// </summary>
      // ------------------------------------------------------------
      public void Connect()
      {
         client = new TcpClient();
         client.Connect(host, port);

         stream = client.GetStream();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Writes raw bytes to the TCP stream.
      /// </summary>
      // ------------------------------------------------------------
      public void Write(byte[] data, int offset, int count)
      {
         stream.Write(data, offset, count);
         stream.Flush();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Reads raw bytes from the TCP stream.
      /// </summary>
      // ------------------------------------------------------------
      public int Read(byte[] buffer, int offset, int count)
      {
         return stream.Read(buffer, offset, count);
      }

   #endregion

   #region IDisposable

      public void Dispose()
      {
         stream?.Dispose();
         client?.Dispose();
      }

   #endregion

   }
}
