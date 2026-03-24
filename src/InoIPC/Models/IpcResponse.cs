using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// IPC response: parse JSON or build success/error responses.
   /// </summary>
   // ============================================================
   public class IpcResponse
   {

   #region Fields

      // ------------------------------------------------------------
      /// <summary>
      /// Whether the response indicates success.
      /// </summary>
      // ------------------------------------------------------------
      public bool IsSuccess { get; set; }

      // ------------------------------------------------------------
      /// <summary>
      /// The raw JSON string of the response.
      /// </summary>
      // ------------------------------------------------------------
      public string RawJson { get; set; }

   #endregion

   #region Parse

      // ------------------------------------------------------------
      /// <summary>
      /// Parses a JSON response string.
      /// </summary>
      // ------------------------------------------------------------
      public static IpcResponse Parse(string json)
      {
         var response = new IpcResponse { RawJson = json };

         try
         {
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("success", out var success))
            {
               response.IsSuccess = success.GetBoolean();
            }
         }
         catch
         {
            response.IsSuccess = false;
         }

         return response;
      }

   #endregion

   #region Success Builders

      // ------------------------------------------------------------
      /// <summary>
      /// Creates an empty success response.
      /// </summary>
      // ------------------------------------------------------------
      public static IpcResponse Success()
      {
         return new IpcResponse
         {
            IsSuccess = true,
            RawJson   = "{\"success\":true}"
         };
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Creates a success response with a message.
      /// </summary>
      // ------------------------------------------------------------
      public static IpcResponse Success(string message)
      {
         var dict = new Dictionary<string, object>
         {
            ["success"] = true,
            ["message"] = message
         };

         return new IpcResponse
         {
            IsSuccess = true,
            RawJson   = JsonSerializer.Serialize(dict)
         };
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Creates a success response with a key-value result.
      /// </summary>
      // ------------------------------------------------------------
      public static IpcResponse Success(string key, object value)
      {
         if (key == "success")
         {
            throw new ArgumentException("Reserved key: 'success'");
         }

         var dict = new Dictionary<string, object>
         {
            ["success"] = true,
            [key]       = value
         };

         return new IpcResponse
         {
            IsSuccess = true,
            RawJson   = JsonSerializer.Serialize(dict)
         };
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Creates a success response from a dictionary.
      /// <br/> Ensures "success" is the first key in the output.
      /// </summary>
      // ----------------------------------------------------------------------
      public static IpcResponse Success(Dictionary<string, object> data)
      {
         var ordered = new Dictionary<string, object> { ["success"] = true };

         foreach (var kv in data)
         {
            if (kv.Key == "success")
            {
               throw new ArgumentException("Reserved key: 'success'");
            }

            ordered[kv.Key] = kv.Value;
         }

         return new IpcResponse
         {
            IsSuccess = true,
            RawJson   = JsonSerializer.Serialize(ordered)
         };
      }

   #endregion

   #region Error Builders

      // ------------------------------------------------------------
      /// <summary>
      /// Creates an error response with code and message.
      /// </summary>
      // ------------------------------------------------------------
      public static IpcResponse Error(string code, string message)
      {
         return Error(code, message, null);
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Creates an error response with code, message, and extra data.
      /// <br/> Extra fields are merged into the error object.
      /// </summary>
      // ----------------------------------------------------------------------
      public static IpcResponse Error(string code, string message, Dictionary<string, object> data)
      {
         var error = new Dictionary<string, object>
         {
            ["code"]    = code,
            ["message"] = message
         };

         if (data != null)
         {
            foreach (var kv in data)
            {
               if (kv.Key == "code" || kv.Key == "message")
               {
                  throw new ArgumentException($"Reserved key: '{kv.Key}'");
               }

               error[kv.Key] = kv.Value;
            }
         }

         var dict = new Dictionary<string, object>
         {
            ["success"] = false,
            ["error"]   = error
         };

         return new IpcResponse
         {
            IsSuccess = false,
            RawJson   = JsonSerializer.Serialize(dict)
         };
      }

   #endregion

   }
}
