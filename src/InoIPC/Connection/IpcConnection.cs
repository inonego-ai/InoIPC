using System;
using System.Threading;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// Wraps ITransport with FrameProtocol for JSON messaging.
   /// Used by both client and server.
   /// </summary>
   // ============================================================
   public class IpcConnection
   {

   #region Fields

      private readonly ITransport transport;

   #endregion

   #region Constructor

      public IpcConnection(ITransport transport)
      {
         this.transport = transport;
      }

   #endregion

   #region Receive

      // ------------------------------------------------------------
      /// <summary>
      /// Receives a JSON string from the transport.
      /// </summary>
      // ------------------------------------------------------------
      public string Receive()
      {
         return FrameProtocol.Receive(transport);
      }

   #endregion

   #region Send

      // ------------------------------------------------------------
      /// <summary>
      /// Sends a raw JSON string to the transport.
      /// </summary>
      // ------------------------------------------------------------
      public void Send(string json)
      {
         FrameProtocol.Send(transport, json);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Sends an IpcResponse to the transport.
      /// </summary>
      // ------------------------------------------------------------
      public void Send(IpcResponse response)
      {
         FrameProtocol.Send(transport, response.RawJson);
      }

   #endregion

   #region Request

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Sends JSON, waits for response, parses as IpcResponse.
      /// <br/> Connects if not already connected.
      /// </summary>
      // ----------------------------------------------------------------------
      public IpcResponse Request(string requestJson)
      {
         if (!transport.IsConnected)
         {
            transport.Connect();
         }

         Send(requestJson);

         string json = Receive();

         return IpcResponse.Parse(json);
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Sends with retry on connection failure.
      /// <br/> Retries until timeout. Interval between retries configurable.
      /// </summary>
      // ----------------------------------------------------------------------
      public IpcResponse RequestWithRetry(string requestJson, int timeoutMs = 30000, int intervalMs = 500)
      {
         var start = DateTime.Now;

         while (true)
         {
            try
            {
               return Request(requestJson);
            }
            catch (Exception)
            {
               if ((DateTime.Now - start).TotalMilliseconds >= timeoutMs)
               {
                  throw new IpcException($"Failed to send after {timeoutMs}ms.");
               }

               Thread.Sleep(intervalMs);
            }
         }
      }

   #endregion

   }
}
