using System;
using System.IO;
using System.Text;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// Length-prefixed frame protocol over ITransport.
   /// Format: [4-byte BE uint32 length][UTF-8 body]
   /// </summary>
   // ============================================================
   public static class FrameProtocol
   {

   #region Send / Receive

      // ------------------------------------------------------------
      /// <summary>
      /// Encodes a string as UTF-8 and writes it as a frame.
      /// </summary>
      // ------------------------------------------------------------
      public static void Send(ITransport transport, string message)
      {
         byte[] body = Encoding.UTF8.GetBytes(message);

         WriteFrame(transport, body);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Reads a frame and decodes it as a UTF-8 string.
      /// </summary>
      // ------------------------------------------------------------
      public static string Receive(ITransport transport)
      {
         byte[] body = ReadFrame(transport);

         return Encoding.UTF8.GetString(body);
      }

   #endregion

   #region Frame I/O

      // ------------------------------------------------------------
      /// <summary>
      /// Writes a length-prefixed frame to the transport.
      /// </summary>
      // ------------------------------------------------------------
      public static void WriteFrame(ITransport transport, byte[] data)
      {
         byte[] header = new byte[4];

         header[0] = (byte)((data.Length >> 24) & 0xFF);
         header[1] = (byte)((data.Length >> 16) & 0xFF);
         header[2] = (byte)((data.Length >> 08) & 0xFF);
         header[3] = (byte)((data.Length >> 00) & 0xFF);

         transport.Write(header, 0, 4);
         transport.Write(data, 0, data.Length);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Reads a length-prefixed frame from the transport.
      /// </summary>
      // ------------------------------------------------------------
      public static byte[] ReadFrame(ITransport transport)
      {
         byte[] header = new byte[4];

         ReadExact(transport, header, 4);

         int length = (header[0] << 24) | 
                      (header[1] << 16) | 
                      (header[2] << 08) |
                      (header[3] << 00);

         byte[] body = new byte[length];

         ReadExact(transport, body, length);

         return body;
      }

   #endregion

   #region Helpers

      // ------------------------------------------------------------
      /// <summary>
      /// Reads exactly count bytes from the transport.
      /// </summary>
      // ------------------------------------------------------------
      private static void ReadExact(ITransport transport, byte[] buffer, int count)
      {
         int offset = 0;

         while (offset < count)
         {
            int read = transport.Read(buffer, offset, count - offset);

            if (read == 0)
            {
               throw new IOException("Connection closed.");
            }

            offset += read;
         }
      }

   #endregion

   }
}
