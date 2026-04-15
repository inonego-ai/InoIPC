using System;
using System.Threading;
using System.Threading.Tasks;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// In-memory server for testing. Pairs with TestTransport.
   /// </summary>
   // ============================================================
   public class TestServer : IServer
   {

   #region Fields

      private Func<IpcConnection, Task> onClient;
      private volatile bool running;

   #endregion

   #region IServer

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Registers the client handler. Does not block.
      /// <br/> Use Accept() to simulate a client connection.
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
      /// <br/> Registers the async client handler. Does not block.
      /// <br/> Use Accept() to simulate a client connection.
      /// </summary>
      // ----------------------------------------------------------------------
      public void Start(Func<IpcConnection, Task> onClient)
      {
         this.onClient = onClient;
         this.running  = true;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Stops the server.
      /// </summary>
      // ------------------------------------------------------------
      public void Stop()
      {
         running  = false;
         onClient = null;
      }

   #endregion

   #region Test Helpers

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Simulates a client connection. Runs the handler with a
      /// <br/> shared TestTransport and returns it for assertion.
      /// </summary>
      // ----------------------------------------------------------------------
      public TestTransport Accept()
      {
         if (!running || onClient == null)
         {
            throw new IpcException("Server not started.");
         }

         var transport  = new TestTransport();
         var connection = new IpcConnection(transport);

         onClient(connection).Wait();

         return transport;
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
