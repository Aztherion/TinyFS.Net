using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace TinyFS.Locking
{
    internal class LockManager : IDisposable
    {
        private static LockManager _instance;
        private readonly ConcurrentDictionary<uint, Lock> _pagelocks = new ConcurrentDictionary<uint,Lock>();
        private readonly object _sync = new object();
        private readonly object _cacheCleanSync = new object();
        private Timer _cacheCleanTimer;
        private bool _disposed;

        public static LockManager Instance { get { return _instance ?? (_instance = new LockManager()); } }

        private LockManager()
        {
            _cacheCleanTimer = new Timer((o) => CollectUnusedPagelocks(10000), null, 1000, 1000);
        }

        public ILock Get(uint handle, LockType lockType)
        {
            lock(_cacheCleanSync)
            {
                switch (lockType)
                {
                    case LockType.Read: return new ReadLock(GetPageLock(handle));
                    case LockType.Write: return new WriteLock(GetPageLock(handle));
                }
                throw new ArgumentException("Invalid LockType", "lockType");                
            }
        }

        public void CollectUnusedPagelocks(int olderThanMilliseconds)
        {
            lock(_cacheCleanSync)
            {
                var timestamp = DateTime.UtcNow.AddMilliseconds(olderThanMilliseconds * -1);
                var toBeRemoved = (from pagelock in _pagelocks where pagelock.Value.IsFreeAndOlderThan(timestamp) select pagelock.Key).ToList();
                Lock dummy;
                toBeRemoved.ForEach(t => _pagelocks.TryRemove(t, out dummy));                
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cacheCleanTimer.Dispose();
            _cacheCleanTimer = null;
        }

        private Lock GetPageLock(uint handle)
        {
            return _pagelocks.GetOrAdd(handle, u =>{ lock (_sync) return new Lock(u);});
        }
    }
}
