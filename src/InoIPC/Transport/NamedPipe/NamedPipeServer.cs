using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// Named Pipe server. Accepts connections and dispatches.
   /// </summary>
   // ============================================================
   public class NamedPipeServer : IServer
   {

   #region Fields

      private readonly string pipeName;

      private volatile bool running;

   #endregion

   #region Constructor

      public NamedPipeServer(string pipeName)
      {
         this.pipeName = pipeName;
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
         running = true;

         while (running)
         {
            try
            {
               var server = new NamedPipeServerStream
               (
                  pipeName, PipeDirection.InOut,
                  NamedPipeServerStream.MaxAllowedServerInstances
               );

               server.WaitForConnection();

               var transport = new NamedPipeTransport(server);

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
            catch (ObjectDisposedException)
            {
               break;
            }
            catch (IOException)
            {
               if (!running) { break; }
            }
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Stops the server.
      /// </summary>
      // ------------------------------------------------------------
      public void Stop()
      {
         running = false;
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
