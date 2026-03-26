using System;
using System.Threading.Tasks;

namespace InoIPC
{
   // ============================================================
   /// <summary>
   /// Server that accepts connections and dispatches to callback.
   /// </summary>
   // ============================================================
   public interface IServer : IDisposable
   {

      void Start(Action<IpcConnection> onClient);

      void Start(Func<IpcConnection, Task> onClient);

      void Stop();

   }
}
