using System;
using System.Collections;
using System.Collections.Generic;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// In-memory transport for testing. Raw byte buffer.
   /// </summary>
   // ============================================================
   public class TestTransport : ITransport
   {

   #region Fields

      public bool IsConnected => true;

      private readonly List<byte> buffer = new List<byte>();
      private int readPos = 0;

   #endregion

   #region ITransport

      public void Connect() {}

      // ------------------------------------------------------------
      /// <summary>
      /// Writes raw bytes to the buffer.
      /// </summary>
      // ------------------------------------------------------------
      public void Write(byte[] data, int offset, int count)
      {
         for (int i = offset; i < offset + count; i++)
         {
            buffer.Add(data[i]);
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Reads raw bytes from the buffer.
      /// </summary>
      // ------------------------------------------------------------
      public int Read(byte[] buf, int offset, int count)
      {
         int available = Math.Min(count, buffer.Count - readPos);

         for (int i = 0; i < available; i++)
         {
            buf[offset + i] = buffer[readPos++];
         }

         return available;
      }

   #endregion

   #region Helpers

      // ------------------------------------------------------------
      /// <summary>
      /// Resets the buffer and read position.
      /// </summary>
      // ------------------------------------------------------------
      public void Reset()
      {
         buffer.Clear();
         readPos = 0;
      }

   #endregion

   #region IDisposable

      public void Dispose() {}

   #endregion

   }
}
