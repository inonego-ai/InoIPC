using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// TCP server. Accepts connections and dispatches.
   /// </summary>
   // ============================================================
   public class TcpServer : IServer
   {

   #region Fields

      private readonly int    port;
      private readonly string host;

      private TcpListener  listener;
      private volatile bool running;

   #endregion

   #region Constructor

      public TcpServer(int port, string host = "127.0.0.1")
      {
         this.port = port;
         this.host = host;
      }

   #endregion

   #region IServer

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Starts accepting connections. Blocks the calling thread.
      /// <br/> Each client is handled in a ThreadPool thread.
      /// </summary>
      // ----------------------------------------------------------------------
      public void Start(Action<IpcConnection> onClient)
      {
         listener = new TcpListener(IPAddress.Parse(host), port);
         listener.Start();

         running = true;

         while (running)
         {
            try
            {
               var client = listener.AcceptTcpClient();

               var transport = new TcpTransport(client);

               ThreadPool.QueueUserWorkItem
               (
                  _ =>
                  {
                     using (transport)
                     {
                        onClient(new IpcConnection(transport));
                     }
                  }
               );
            }
            catch (SocketException)
            {
               if (!running) { break; }
            }
            catch (ObjectDisposedException)
            {
               break;
            }
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Stops the server and closes the listener.
      /// </summary>
      // ------------------------------------------------------------
      public void Stop()
      {
         running = false;
         listener?.Stop();
      }

   #endregion

   #region IDisposable

      public void Dispose()
      {
         Stop();
      }

   #endregion

   }
}
