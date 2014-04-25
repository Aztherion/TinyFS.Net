namespace TinyFS.Locking
{
    internal class ReadLock : ILock
    {
        private readonly Lock _pageLock;

        public ReadLock(Lock pageLock)
        {
            _pageLock = pageLock;
            _pageLock.GetRead();
        }

        public void Dispose()
        {
            _pageLock.FreeRead();
        }
    }
}
