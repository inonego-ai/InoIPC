using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

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

      private CancellationTokenSource cts;

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
      /// <br/> Each client is handled synchronously in a ThreadPool thread.
      /// </summary>
      // ----------------------------------------------------------------------
      public void Start(Action<IpcConnection> onClient)
      {
         Start(conn =>
         {
            onClient(conn);
            return Task.CompletedTask;
         });
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Starts accepting connections. Blocks the calling thread.
      /// <br/> Each client is handled asynchronously — transport stays
      /// <br/> alive until the returned Task completes.
      /// </summary>
      // ----------------------------------------------------------------------
      public void Start(Func<IpcConnection, Task> onClient)
      {
         cts = new CancellationTokenSource();

         while (!cts.IsCancellationRequested)
         {
            NamedPipeServerStream server = null;

            try
            {
               server = new NamedPipeServerStream
               (
                  pipeName, PipeDirection.InOut,
                  NamedPipeServerStream.MaxAllowedServerInstances
               );

               server.WaitForConnectionAsync(cts.Token).Wait();

               var transport = new NamedPipeTransport(server);

               _ = Task.Run(async () =>
               {
                  using (transport)
                  {
                     await onClient(new IpcConnection(transport));
                  }
               });
            }
            catch (OperationCanceledException)
            {
               server?.Dispose();
               break;
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
               server?.Dispose();
               break;
            }
            catch (IOException)
            {
               server?.Dispose();

               if (cts.IsCancellationRequested) { break; }
            }
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Stops the server by cancelling the accept loop.
      /// </summary>
      // ------------------------------------------------------------
      public void Stop()
      {
         cts?.Cancel();
      }

   #endregion

   #region IDisposable

      public void Dispose()
      {
         Stop();
         cts?.Dispose();
      }

   #endregion

   }
}
