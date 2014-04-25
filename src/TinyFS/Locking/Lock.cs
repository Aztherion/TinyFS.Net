using System;
using System.Threading;

namespace TinyFS.Locking
{
    internal class Lock
    {
        private readonly ManualResetEvent _readLock;
        private readonly ManualResetEvent _writeLock;
        private readonly object _sync; 
        private int _readRefCount;
        private DateTime _timestamp;

        public uint Handle { get; set; }
        
        public Lock(uint handle)
        {
            Handle = handle;
            _readRefCount = 0;
            _sync = new object();
            _readLock = new ManualResetEvent(true);
            _writeLock = new ManualResetEvent(true);
            _timestamp = DateTime.UtcNow;
        }

        public void GetRead()
        {
            _writeLock.WaitOne();
            lock (_sync)
            {
                _readRefCount++;
                if (_readRefCount == 1) _readLock.Reset();
                _timestamp = DateTime.UtcNow;
            }
        }

        public void FreeRead()
        {
            lock(_sync)
            {
                _readRefCount--;
                if (_readRefCount == 0) _readLock.Set();
            }
        }

        public void GetWrite()
        {
            while(true)
            {
                _writeLock.WaitOne();
                lock (_sync)
                {
                    if (!_writeLock.WaitOne(0))
                    {
                        Console.WriteLine("RETRY");
                        continue;
                    }
                    _writeLock.Reset();
                    break;
                }
            }
            _readLock.WaitOne();
            _timestamp = DateTime.UtcNow;                
        }

        public void FreeWrite()
        {
            lock(_sync)
            {
                _writeLock.Set();
            }
        }

        public bool IsFreeAndOlderThan(DateTime timestamp)
        {
            lock(_sync)
            {
                return _readRefCount == 0 && _writeLock.WaitOne(0) && _timestamp < timestamp;
            }
        }
    }
}
