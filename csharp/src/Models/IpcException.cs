using System;

namespace InoIPC
{
   // ==================================================================
   /// <summary>
   /// Exception thrown for IPC errors.
   /// </summary>
   // ==================================================================
   public class IpcException : Exception
   {

   #region Constructors

      public IpcException(string message) : base(message) {}

      public IpcException(string message, Exception inner) : base(message, inner) {}

   #endregion

   }
}
