using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Xunit;

using InoIPC;

namespace InoIPC.TEST
{
   // ============================================================
   /// <summary>
   /// Tests for FrameProtocol round-trip via TestTransport.
   /// </summary>
   // ============================================================
   public class TEST_FrameProtocol
   {

   #region Round-trip

      [Fact]
      public void SendReceive_RoundTrip()
      {
         var transport = new TestTransport();

         FrameProtocol.Send(transport, "{\"test\":\"ping\"}");

         string result = FrameProtocol.Receive(transport);

         Assert.Equal("{\"test\":\"ping\"}", result);
      }

      [Fact]
      public void SendReceive_EmptyBody()
      {
         var transport = new TestTransport();

         FrameProtocol.Send(transport, "");

         string result = FrameProtocol.Receive(transport);

         Assert.Equal("", result);
      }

      [Fact]
      public void SendReceive_Unicode()
      {
         var transport = new TestTransport();
         string input  = "{\"msg\":\"한글 테스트 🎉\"}";

         FrameProtocol.Send(transport, input);

         string result = FrameProtocol.Receive(transport);

         Assert.Equal(input, result);
      }

      [Fact]
      public void SendReceive_LargePayload()
      {
         var transport = new TestTransport();
         string input  = new string('x', 100_000);

         FrameProtocol.Send(transport, input);

         string result = FrameProtocol.Receive(transport);

         Assert.Equal(input, result);
      }

      [Fact]
      public void SendReceive_MultipleFrames()
      {
         var transport = new TestTransport();

         FrameProtocol.Send(transport, "first");
         FrameProtocol.Send(transport, "second");
         FrameProtocol.Send(transport, "third");

         Assert.Equal("first", FrameProtocol.Receive(transport));
         Assert.Equal("second", FrameProtocol.Receive(transport));
         Assert.Equal("third", FrameProtocol.Receive(transport));
      }

   #endregion

   }
}
