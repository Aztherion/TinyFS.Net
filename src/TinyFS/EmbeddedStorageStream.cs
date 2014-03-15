using System;
using System.IO;

namespace TinyFS
{
    public class EmbeddedStorageStream : Stream
    {
        private long _position;
        private FileInfo _info;
        private readonly EmbeddedStorage _storage;

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return _info.Length; }
        }

        public override long Position { get { return _position; } set { _position = value; } }        

        public EmbeddedStorageStream(string filename, EmbeddedStorage embeddedStorage)
        {
            _storage = embeddedStorage;
            if (!_storage.Exists(filename))
            {
                _storage.CreateFile(filename);
            }
            _info = _storage.Files().Find(t => t.Name.Equals(filename));
            if (_info == null) throw new IOException("filename invalid");
        }

        public override void Flush() {}

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset > _info.Length) throw new IOException("offset out of range");
                    _position = offset;
                    break;
                case SeekOrigin.End:
                    if (offset > _info.Length) throw new IOException("offset out of range");
                    _position = _info.Length - offset;
                    break;
                case SeekOrigin.Current:
                    if (_position + offset > _info.Length) throw new IOException("offset out of range");
                    _position += offset;
                    break;
            }
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position == _info.Length) return -1;
            if (buffer.Length < offset + count) throw new Exception("buffer too small");
            var ib = new byte[count];
            var bytesRead = (int) _storage.ReadAt(_info, ib, (uint)_position, (uint)count);
            Buffer.BlockCopy(ib, 0, buffer, offset, bytesRead);
            _position += bytesRead;
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _storage.WriteAt(_info, buffer, offset, count, (uint)_position);
            _info = _storage.Files().Find(t => t.Name.Equals(_info.Name));
        }

    }
}
