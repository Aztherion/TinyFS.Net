using System.IO;

namespace TinyFS
{
    public class StreamWrapper : Stream
    {
        private Stream _stream;

        protected virtual Stream Stream { get { return _stream; } set { _stream = value; } }

        protected StreamWrapper(Stream stream)
        {
            _stream = stream;
        }

        public override bool CanRead { get { return Stream.CanRead; } }
        public override bool CanSeek { get { return Stream.CanSeek; } }
        public override bool CanWrite { get { return Stream.CanWrite; } }

        public override long Length { get { return Stream.Length; } }

        public override long Position
        {
            get { return Stream.Position; }
            set { Stream.Position = value; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            if (_stream != null)
                _stream.Close();

            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _stream != null)
                _stream.Dispose();
            _stream = null;

            base.Dispose(disposing);
        }

        public override void Flush()
        {
            Stream.Flush();
        }
    }
}
