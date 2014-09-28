using System;
using System.IO;
using System.Threading;

namespace TinyFS
{
    public interface IStreamCache : IDisposable
    {
        Stream Open(FileAccess fileAccess);

    }
    class StreamCache : IStreamCache
    {
        private readonly IFileStreamFactory _streamFactory;
        private readonly Mutex[] _handles;
        private readonly Stream[] _streams;
        private RefInt[] _refCount;
        private bool _disposed;

        public StreamCache(IFileStreamFactory streamFactory)
            : this(streamFactory, Environment.ProcessorCount) { }

        public StreamCache(IFileStreamFactory streamFactory, int maxHandleCount)
        {
            _streamFactory = streamFactory;
            _handles = new Mutex[maxHandleCount];
            _streams = new Stream[maxHandleCount];
            _refCount = new RefInt[maxHandleCount];
            InitializeMutexes();
            InitializeRefcount();
        }

        public Stream Open(FileAccess fileAccess)
        {
            int ix;
            try
            {
                ix = WaitHandle.WaitAny(_handles);
            }
            catch (AbandonedMutexException aex)
            {
                // not very intuitive, but MSDN has this to say: 
                // "An AbandonedMutexException is thrown when one thread acquires a Mutex that another thread has abondoned by exiting without releasing it."
                // This means that the thread in which the exception is thrown is the new owner of said Mutex Object. Since the data structure (stream) that we 
                // protect with the mutex is guaranteed to be in a safe state we can simply(!) ignore the exception and use the Mutex as if nothing has happened. 
                ix = aex.MutexIndex;
            }
            var stream = _streams[ix];
            if (stream == null || !stream.CanRead)
            {
                _streams[ix] = stream = _streamFactory.Create();
            }
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            return new CachedStream(this, stream, fileAccess, _handles[ix], _refCount[ix]);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            _disposed = true;
            var hx = _handles.Length;
            for (var i = 0; i < hx; i++)
            {
                _handles[i].Close();
            }
            int sx = _streams.Length;
            for (var i = 0; i < sx; i++)
            {
                if (_streams[i] != null)
                {
                    _streams[i].Dispose();
                }
            }
        }

        private void InitializeMutexes()
        {
            var ix = _handles.Length;
            for (var i = 0; i < ix; i++)
                _handles[i] = new Mutex();
        }

        private void InitializeRefcount()
        {
            for (var i = 0; i < _handles.Length; i++)
                _refCount[i] = new RefInt();
        }

        private void ResetHandle(Mutex handle)
        {
            var ix = _handles.Length;
            for (var i = 0; i < ix; i++)
            {
                if (!ReferenceEquals(_handles[i], handle)) continue;
                _handles[i] = new Mutex();
                break;
            }
        }

        private class RefInt
        {
            public int Value { get; set; }

            public RefInt()
            {
                Value = 0;
            }
        }

        private class CachedStream : StreamWrapper
        {
            private const FileAccess NO_ACCESS = 0;
            private readonly StreamCache _cache;
            private readonly Mutex _handle;
            private FileAccess _fileAccess;
            private RefInt _refCount;

            public override bool CanRead { get { return (_fileAccess & FileAccess.Read) == FileAccess.Read && Stream.CanRead; } }
            public override bool CanSeek { get { return _fileAccess != NO_ACCESS && Stream.CanSeek; } }
            public override bool CanWrite { get { return (_fileAccess & FileAccess.Write) == FileAccess.Write && Stream.CanWrite; } }

            public CachedStream(StreamCache cache, Stream stream, FileAccess fileaccess, Mutex handle, RefInt refCount)
                : base(stream)
            {
                _cache = cache;
                _handle = handle;
                _fileAccess = fileaccess;
                _refCount = refCount;
                _refCount.Value++;
            }

            //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly")]
            ~CachedStream()
            {
                Dispose(false);
            }

            public override void SetLength(long value)
            {
                CheckDisposed();
                base.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                CheckDisposed();
                return base.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                CheckDisposed();
                base.Write(buffer, offset, count);
            }

            public override void Close()
            {
                Dispose(true);
            }

            protected override void Dispose(bool disposing)
            {
                if (--_refCount.Value != 0) return;
                    base.Dispose(disposing);

                if (_fileAccess != NO_ACCESS)
                {
                    _fileAccess = NO_ACCESS;

                    if (disposing)
                    {
                        try { _handle.ReleaseMutex(); }
                        catch (ObjectDisposedException) { }
                    }
                    else
                        _cache.ResetHandle(_handle);
                }
                Stream = null;
            }

            private void CheckDisposed()
            {
                if (_fileAccess == NO_ACCESS)
                    throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
