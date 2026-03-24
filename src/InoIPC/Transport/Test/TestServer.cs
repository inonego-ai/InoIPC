using System;
using System.Threading;

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

      private Action<IpcConnection> onClient;
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

         onClient(connection);

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
