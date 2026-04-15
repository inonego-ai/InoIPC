using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using InoIPC;

namespace InoIPC.TEST
{
   // ============================================================
   /// <summary>
   /// Tests for IpcConnection: send, receive, request pattern.
   /// </summary>
   // ============================================================
   public class TEST_IpcConnection
   {

   #region Send / Receive

      [Fact]
      public void Send_Receive_RoundTrip()
      {
         var transport = new TestTransport();
         var conn      = new IpcConnection(transport);

         conn.Send("{\"test\":\"ping\"}");

         string result = conn.Receive();

         Assert.Equal("{\"test\":\"ping\"}", result);
      }

      [Fact]
      public void Send_IpcResponse()
      {
         var transport = new TestTransport();
         var conn      = new IpcConnection(transport);

         conn.Send(IpcResponse.Success("ok"));

         string result = conn.Receive();

         Assert.Contains("\"success\":true", result);
         Assert.Contains("\"message\":\"ok\"", result);
      }

   #endregion

   #region Request

      [Fact]
      public void Request_ParsesResponse()
      {
         var transport = new TestTransport();

         // Simulate server response in buffer
         FrameProtocol.Send(transport, "{\"success\":true,\"value\":42}");

         var conn     = new IpcConnection(transport);
         var response = conn.Request("{\"action\":\"get\"}");

         Assert.True(response.IsSuccess);
      }

   #endregion

   #region Server Integration

      [Fact]
      public void Server_Sends_Client_Receives()
      {
         var transport = new TestTransport();

         // Server writes
         var serverConn = new IpcConnection(transport);
         serverConn.Send(IpcResponse.Success("ready"));

         // Client reads from same buffer
         var clientConn = new IpcConnection(transport);
         string response = clientConn.Receive();

         Assert.Contains("\"success\":true", response);
         Assert.Contains("\"message\":\"ready\"", response);
      }

      [Fact]
      public void TestServer_Callback_Receives_IpcConnection()
      {
         var server   = new TestServer();
         bool called  = false;

         server.Start(conn =>
         {
            called = true;
            Assert.NotNull(conn);
         });

         server.Accept();

         Assert.True(called);
      }

   #endregion

   }
}
