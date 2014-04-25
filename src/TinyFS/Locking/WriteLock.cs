namespace TinyFS.Locking
{
    internal class WriteLock : ILock
    {
        private readonly Lock _pageLock;

        public WriteLock(Lock pageLock)
        {
            _pageLock = pageLock;
            _pageLock.GetWrite();
        }

        public void Dispose()
        {
            _pageLock.FreeWrite();
        }
    }
}
