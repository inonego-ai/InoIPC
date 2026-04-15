using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Encodings;
using System.Text.Encodings.Web;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// JSON read/write utilities. Abstracts the underlying JSON
   /// engine (System.Text.Json on .NET 8+, Newtonsoft otherwise).
   /// </summary>
   // ============================================================
   public static class JsonHelper
   {

   #region Serialize

      // ------------------------------------------------------------
      /// <summary>
      /// Serializes a dictionary to a JSON string.
      /// </summary>
      // ------------------------------------------------------------
      public static string Serialize(Dictionary<string, object> dict)
      {
#if NET8_0_OR_GREATER
         return JsonSerializer.Serialize(dict);
#else
         return JsonConvert.SerializeObject(dict);
#endif
      }

   #endregion

   #region Parse

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Parses a JSON string and extracts the "success" boolean.
      /// <br/> Returns false if parsing fails or "success" is missing.
      /// </summary>
      // ----------------------------------------------------------------------
      public static bool ParseSuccess(string json)
      {
         try
         {
#if NET8_0_OR_GREATER
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("success", out var success))
            {
               return success.GetBoolean();
            }
#else
            var obj = JObject.Parse(json);

            if (obj.TryGetValue("success", out var success))
            {
               return success.Value<bool>();
            }
#endif
         }
         catch {}

         return false;
      }

   #endregion

   #region Read — Int

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Extracts an int from a JSON value.
      /// <br/> Handles both number and string representations.
      /// </summary>
      // ----------------------------------------------------------------------
#if NET8_0_OR_GREATER
      public static int GetInt(JsonElement element, int fallback = 0)
      {
         if (element.ValueKind == JsonValueKind.Number)
         {
            return element.GetInt32();
         }

         if (element.ValueKind == JsonValueKind.String)
         {
            if (int.TryParse(element.GetString(), out int val))
            {
               return val;
            }
         }

         return fallback;
      }
#else
      public static int GetInt(JToken token, int fallback = 0)
      {
         if (token == null || token.Type == JTokenType.Null)
         {
            return fallback;
         }

         if (token.Type == JTokenType.Integer)
         {
            return token.Value<int>();
         }

         if (token.Type == JTokenType.String)
         {
            if (int.TryParse(token.Value<string>(), out int val))
            {
               return val;
            }
         }

         return fallback;
      }
#endif

   #endregion

   #region Read — Long

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Extracts a long from a JSON value.
      /// <br/> Handles both number and string representations.
      /// </summary>
      // ----------------------------------------------------------------------
#if NET8_0_OR_GREATER
      public static long GetLong(JsonElement element, long fallback = 0)
      {
         if (element.ValueKind == JsonValueKind.Number)
         {
            return element.GetInt64();
         }

         if (element.ValueKind == JsonValueKind.String)
         {
            if (long.TryParse(element.GetString(), out long val))
            {
               return val;
            }
         }

         return fallback;
      }
#else
      public static long GetLong(JToken token, long fallback = 0)
      {
         if (token == null || token.Type == JTokenType.Null)
         {
            return fallback;
         }

         if (token.Type == JTokenType.Integer)
         {
            return token.Value<long>();
         }

         if (token.Type == JTokenType.String)
         {
            if (long.TryParse(token.Value<string>(), out long val))
            {
               return val;
            }
         }

         return fallback;
      }
#endif

   #endregion

   #region Read — Float

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Extracts a float from a JSON value.
      /// <br/> Handles both number and string representations.
      /// </summary>
      // ----------------------------------------------------------------------
#if NET8_0_OR_GREATER
      public static float GetFloat(JsonElement element, float fallback = 0f)
      {
         if (element.ValueKind == JsonValueKind.Number)
         {
            return element.GetSingle();
         }

         if (element.ValueKind == JsonValueKind.String)
         {
            if (float.TryParse(element.GetString(), out float val))
            {
               return val;
            }
         }

         return fallback;
      }
#else
      public static float GetFloat(JToken token, float fallback = 0f)
      {
         if (token == null || token.Type == JTokenType.Null)
         {
            return fallback;
         }

         if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
         {
            return token.Value<float>();
         }

         if (token.Type == JTokenType.String)
         {
            if (float.TryParse(token.Value<string>(), out float val))
            {
               return val;
            }
         }

         return fallback;
      }
#endif

   #endregion

   #region Read — Double

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Extracts a double from a JSON value.
      /// <br/> Handles both number and string representations.
      /// </summary>
      // ----------------------------------------------------------------------
#if NET8_0_OR_GREATER
      public static double GetDouble(JsonElement element, double fallback = 0.0)
      {
         if (element.ValueKind == JsonValueKind.Number)
         {
            return element.GetDouble();
         }

         if (element.ValueKind == JsonValueKind.String)
         {
            if (double.TryParse(element.GetString(), out double val))
            {
               return val;
            }
         }

         return fallback;
      }
#else
      public static double GetDouble(JToken token, double fallback = 0.0)
      {
         if (token == null || token.Type == JTokenType.Null)
         {
            return fallback;
         }

         if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
         {
            return token.Value<double>();
         }

         if (token.Type == JTokenType.String)
         {
            if (double.TryParse(token.Value<string>(), out double val))
            {
               return val;
            }
         }

         return fallback;
      }
#endif

   #endregion

   #region Read — String

      // ------------------------------------------------------------
      /// <summary>
      /// Extracts a string from a JSON value with fallback.
      /// </summary>
      // ------------------------------------------------------------
#if NET8_0_OR_GREATER
      public static string GetString(JsonElement element, string fallback = null)
      {
         if (element.ValueKind == JsonValueKind.String)
         {
            return element.GetString();
         }

         return fallback;
      }
#else
      public static string GetString(JToken token, string fallback = null)
      {
         if (token == null || token.Type == JTokenType.Null)
         {
            return fallback;
         }

         if (token.Type == JTokenType.String)
         {
            return token.Value<string>();
         }

         return fallback;
      }
#endif

   #endregion

   #region Read — Bool

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Extracts a bool from a JSON value.
      /// <br/> Handles bool, string ("true"/"false"), and number (0/1).
      /// </summary>
      // ----------------------------------------------------------------------
#if NET8_0_OR_GREATER
      public static bool GetBool(JsonElement element, bool fallback = false)
      {
         if (element.ValueKind == JsonValueKind.True)
         {
            return true;
         }

         if (element.ValueKind == JsonValueKind.False)
         {
            return false;
         }

         if (element.ValueKind == JsonValueKind.String)
         {
            if (bool.TryParse(element.GetString(), out bool val))
            {
               return val;
            }
         }

         if (element.ValueKind == JsonValueKind.Number)
         {
            return element.GetInt32() != 0;
         }

         return fallback;
      }
#else
      public static bool GetBool(JToken token, bool fallback = false)
      {
         if (token == null || token.Type == JTokenType.Null)
         {
            return fallback;
         }

         if (token.Type == JTokenType.Boolean)
         {
            return token.Value<bool>();
         }

         if (token.Type == JTokenType.String)
         {
            if (bool.TryParse(token.Value<string>(), out bool val))
            {
               return val;
            }
         }

         if (token.Type == JTokenType.Integer)
         {
            return token.Value<int>() != 0;
         }

         return fallback;
      }
#endif

   #endregion

   #region Write

      // ------------------------------------------------------------
      /// <summary>
      /// Writes JSON to stdout, optionally pretty-printed.
      /// </summary>
      // ------------------------------------------------------------
      public static void Write(string json, bool pretty = false)
      {
         if (pretty)
         {
            json = Prettify(json);
         }

         Console.WriteLine(json);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Writes JSON to stderr, optionally pretty-printed.
      /// </summary>
      // ------------------------------------------------------------
      public static void WriteError(string json, bool pretty = false)
      {
         if (pretty)
         {
            json = Prettify(json);
         }

         Console.Error.WriteLine(json);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Re-formats a JSON string with indentation.
      /// </summary>
      // ------------------------------------------------------------
#if NET8_0_OR_GREATER
      private static readonly JsonSerializerOptions PrettyOptions = new JsonSerializerOptions
      {
         WriteIndented = true,
         Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
      };
#endif

      public static string Prettify(string json)
      {
#if NET8_0_OR_GREATER
         var doc = JsonDocument.Parse(json);

         return JsonSerializer.Serialize(doc, PrettyOptions);
#else
         var obj = JToken.Parse(json);

         return obj.ToString(Formatting.Indented);
#endif
      }

   #endregion

   }
}
