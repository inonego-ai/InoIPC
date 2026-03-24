using System;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// Raw byte stream transport abstraction.
   /// </summary>
   // ============================================================
   public interface ITransport : IDisposable
   {

      bool IsConnected { get; }

      void Connect();

      void Write(byte[] data, int offset, int count);
      int Read(byte[] buffer, int offset, int count);

   }
}
