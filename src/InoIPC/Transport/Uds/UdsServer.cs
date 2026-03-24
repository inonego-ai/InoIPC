using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace InoIPC
{
   // ======================================================================
   /// <summary>
   /// Unix Domain Socket server. Accepts connections and dispatches.
   /// </summary>
   // ======================================================================
   public class UdsServer : IServer
   {

   #region Fields

      private readonly string socketPath;

      private Socket   listener;
      private volatile bool running;

   #endregion

   #region Constructor

      public UdsServer(string socketPath)
      {
         this.socketPath = socketPath;
      }

   #endregion

   #region IServer

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Starts accepting connections. Blocks the calling thread.
      /// <br/> Each client is handled in a ThreadPool thread.
      /// <br/> Cleans up socket file on stop.
      /// </summary>
      // ----------------------------------------------------------------------
      public void Start(Action<IpcConnection> onClient)
      {
         // Clean stale socket file
         if (File.Exists(socketPath))
         {
            File.Delete(socketPath);
         }

         var endpoint = new UnixDomainSocketEndPoint(socketPath);

         listener = new Socket
         (
            AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified
         );
         listener.Bind(endpoint);
         listener.Listen(8);

         running = true;

         while (running)
         {
            try
            {
               var client = listener.Accept();

               var transport = new UdsTransport(client);

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
      /// Stops the server and cleans up socket file.
      /// </summary>
      // ------------------------------------------------------------
      public void Stop()
      {
         running = false;

         listener?.Close();
         listener?.Dispose();

         if (File.Exists(socketPath))
         {
            File.Delete(socketPath);
         }
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
