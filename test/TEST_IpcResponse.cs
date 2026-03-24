using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using InoIPC;

namespace InoIPC.TEST
{
   // ============================================================
   /// <summary>
   /// Tests for IpcResponse builders.
   /// </summary>
   // ============================================================
   public class TEST_IpcResponse
   {

   #region Success

      [Fact]
      public void Success_Empty()
      {
         var r = IpcResponse.Success();

         Assert.True(r.IsSuccess);
         Assert.Contains("\"success\":true", r.RawJson);
      }

      [Fact]
      public void Success_Message()
      {
         var r = IpcResponse.Success("Connected");

         Assert.True(r.IsSuccess);
         Assert.Contains("\"message\":\"Connected\"", r.RawJson);
      }

      [Fact]
      public void Success_KeyValue()
      {
         var r = IpcResponse.Success("port", 56400);

         Assert.True(r.IsSuccess);
         Assert.Contains("\"port\":56400", r.RawJson);
      }

      [Fact]
      public void Success_Dict()
      {
         var r = IpcResponse.Success(new Dictionary<string, object>
         {
            ["port"] = 56400,
            ["host"] = "localhost"
         });

         Assert.True(r.IsSuccess);
         Assert.Contains("\"port\":56400", r.RawJson);
         Assert.Contains("\"host\":\"localhost\"", r.RawJson);
      }

      [Fact]
      public void Success_KeyValue_ReservedKey_Throws()
      {
         Assert.Throws<ArgumentException>(() => IpcResponse.Success("success", true));
      }

      [Fact]
      public void Success_Dict_ReservedKey_Throws()
      {
         Assert.Throws<ArgumentException>(() => IpcResponse.Success(new Dictionary<string, object>
         {
            ["success"] = true
         }));
      }

   #endregion

   #region Error

      [Fact]
      public void Error_CodeMessage()
      {
         var r = IpcResponse.Error("TIMEOUT", "timed out");

         Assert.False(r.IsSuccess);
         Assert.Contains("\"code\":\"TIMEOUT\"", r.RawJson);
         Assert.Contains("\"message\":\"timed out\"", r.RawJson);
      }

      [Fact]
      public void Error_WithData()
      {
         var r = IpcResponse.Error("TIMEOUT", "timed out", new Dictionary<string, object>
         {
            ["elapsed"] = 5000
         });

         Assert.False(r.IsSuccess);
         Assert.Contains("\"elapsed\":5000", r.RawJson);
      }

      [Fact]
      public void Error_Data_ReservedKey_Throws()
      {
         Assert.Throws<ArgumentException>(() => IpcResponse.Error("X", "x", new Dictionary<string, object>
         {
            ["code"] = "override"
         }));
      }

   #endregion

   #region Parse

      [Fact]
      public void Parse_Success()
      {
         var r = IpcResponse.Parse("{\"success\":true,\"port\":56400}");

         Assert.True(r.IsSuccess);
      }

      [Fact]
      public void Parse_Error()
      {
         var r = IpcResponse.Parse("{\"success\":false,\"error\":{\"code\":\"X\"}}");

         Assert.False(r.IsSuccess);
      }

      [Fact]
      public void Parse_InvalidJson()
      {
         var r = IpcResponse.Parse("not json");

         Assert.False(r.IsSuccess);
      }

   #endregion

   }
}
