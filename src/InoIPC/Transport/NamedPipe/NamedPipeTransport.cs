using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// Named Pipe transport. Raw byte stream over Named Pipe.
   /// </summary>
   // ============================================================
   public class NamedPipeTransport : ITransport
   {

   #region Fields

      public bool IsConnected => stream != null && stream.IsConnected;

      private readonly string pipeName;

      private PipeStream stream;

   #endregion

   #region Constructors

      // ------------------------------------------------------------
      /// <summary>
      /// Creates a transport that will connect to a named pipe.
      /// </summary>
      // ------------------------------------------------------------
      public NamedPipeTransport(string pipeName)
      {
         this.pipeName = pipeName;
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Wraps an already-connected pipe stream (e.g. from server).
      /// <br/> Connect() becomes a no-op.
      /// </summary>
      // ----------------------------------------------------------------------
      public NamedPipeTransport(PipeStream acceptedStream)
      {
         this.pipeName = null;
         this.stream   = acceptedStream;
      }

   #endregion

   #region ITransport

      // ------------------------------------------------------------
      /// <summary>
      /// Connects to the named pipe server (default 5s timeout).
      /// </summary>
      // ------------------------------------------------------------
      public void Connect()
      {
         Connect(5000);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Connects to the named pipe server with timeout.
      /// </summary>
      // ------------------------------------------------------------
      public void Connect(int timeoutMs)
      {
         if (stream != null)
         {
            return;
         }

         var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
         client.Connect(timeoutMs);

         stream = client;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Writes raw bytes to the pipe stream.
      /// </summary>
      // ------------------------------------------------------------
      public void Write(byte[] data, int offset, int count)
      {
         stream.Write(data, offset, count);
         stream.Flush();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Reads raw bytes from the pipe stream.
      /// </summary>
      // ------------------------------------------------------------
      public int Read(byte[] buffer, int offset, int count)
      {
         return stream.Read(buffer, offset, count);
      }

   #endregion

   #region IDisposable

      public void Dispose()
      {
         stream?.Close();
         stream?.Dispose();
      }

   #endregion
   
   #region Discovery

      // ------------------------------------------------------------
      /// <summary>
      /// Finds the first active pipe matching the prefix, or null.
      /// </summary>
      // ------------------------------------------------------------
      public static string Find(string prefix)
      {
         var all = FindAll(prefix);

         return all.Count > 0 ? all[0] : null;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Finds all active pipes matching the prefix.
      /// </summary>
      // ------------------------------------------------------------
      public static List<string> FindAll(string prefix)
      {
         var result = new List<string>();

         try
         {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
               foreach (var file in Directory.GetFiles(@"\\.\pipe\", prefix + "*"))
               {
                  result.Add(Path.GetFileName(file));
               }
            }
            else
            {
               string unixPrefix = "CoreFxPipe_" + prefix;

               foreach (var file in Directory.GetFiles("/tmp", unixPrefix + "*"))
               {
                  result.Add(Path.GetFileName(file).Substring("CoreFxPipe_".Length));
               }
            }
         }
         catch (Exception) { }

         return result;
      }

   #endregion

   }
}
