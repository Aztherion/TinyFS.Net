using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TinyFS.Test
{
    [TestClass]
    public class CompoundFileTest
    {
        [TestMethod]
        public void Constructor()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var file = new CompoundFile(path))
            {
                Assert.IsNotNull(file);
                Assert.IsTrue(File.Exists(path));
                Assert.IsTrue(new System.IO.FileInfo(path).Length > 0);
            }
            using (var file2 = new CompoundFile(path))
            {
                Assert.IsNotNull(file2);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void Allocate_Test()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var file = new CompoundFile(path))
            {
                var ix = file.Allocate();
                Assert.AreEqual((uint)1, ix);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void Allocate_255Bytes()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var file = new CompoundFile(path))
            {
                var ix = file.Allocate(255);
                Assert.AreEqual((uint) 1, ix);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void Allocate_6000Bytes()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var file = new CompoundFile(path))
            {
                var ix = file.Allocate(6000);
                Assert.AreEqual((uint) 1, ix);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void Allocate_FullChapter_Test()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var file = new CompoundFile(path))
            {
                for (int i=0;i<4096;i++)
                {
                    var ix = file.Allocate();
                }
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void Allocate_Write_SinglePage()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var file = new CompoundFile(path))
            {
                var ix = file.Allocate();
                var data = new byte[255];
                for (byte i = 0; i < 255; i++)
                    data[i] = i;
                file.Write(ix, data, 0, 255);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void Allocate_Write_TwoPages()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var file = new CompoundFile(path))
            {
                var ix = file.Allocate();
                var data = new byte[6000];
                for (int i = 0; i < 6000; i++)
                    data[i] = (byte) (i%255);
                file.Write(ix, data, 0, 6000);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void Allocate_Write_Read_SinglePage()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var file = new CompoundFile(path))
            {
                var ix = file.Allocate();
                var data = new byte[255];
                for (byte i = 0; i < 255; i++) data[i] = i;
                file.Write(ix, data, 0, 255);
                var readData = file.ReadAll(ix);
                for(byte i=0;i<255;i++)
                    Assert.AreEqual(data[i], readData[i]);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void Allocate_Write_Read_TwoPages()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var file = new CompoundFile(path))
            {
                var ix = file.Allocate();
                var data = new byte[6000];
                for (int i = 0; i < 6000; i++)
                    data[i] = (byte)(i % 255);
                file.Write(ix, data, 0, 6000);
                var readdata = file.ReadAll(ix);
                for(int i=0;i<6000;i++)
                    Assert.AreEqual(data[i], readdata[i]);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void InitializeFile_VerifyLength()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf)) { }
            var stream = msf.Stream;
            Assert.AreEqual(4096 * 4096, stream.Length);
            stream.AllowDispose = true;
            stream.Dispose();
        }

        [TestMethod]
        public void InitializeFile_VerifyHeader()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf)) { }
            var stream = msf.Stream;
            var buffer = new byte[4];

            stream.Position = 50;
            stream.Read(buffer, 0, 2);
            ushort version = System.BitConverter.ToUInt16(buffer, 0);
            Assert.AreEqual((ushort)1, version);

            stream.Position = 52;
            stream.Read(buffer, 0, 2);
            ushort pageSize = System.BitConverter.ToUInt16(buffer, 0);
            Assert.AreEqual((ushort)4096, pageSize);

            stream.Position = 54;
            stream.Read(buffer, 0, 2);
            ushort chapterSize = System.BitConverter.ToUInt16(buffer, 0);
            Assert.AreEqual((ushort)4096, chapterSize);

            stream.Position = 60;
            stream.Read(buffer, 0, 4);
            uint firstFreePage = System.BitConverter.ToUInt32(buffer, 0);
            Assert.AreEqual((uint)1, firstFreePage);
            
            stream.AllowDispose = true;
            stream.Dispose();
        }

        [TestMethod]
        public void InitializeFile_Validate_ExpectsOk()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf)) 
            {
                Assert.IsTrue(file.ValidateCrc());
            }
            var stream = msf.Stream;
            stream.AllowDispose = true;
            stream.Dispose();
        }

        [TestMethod]
        public void InitializeFile_ReadAt_ExpectsOk()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf))
            {
                var handle = file.Allocate();
                var data = new byte[255];
                for (byte i = 0; i < 255; i++) data[i] = i;
                file.Write(handle, data, 0, 255);

                var readData = new byte[10];
                file.ReadAt(handle, readData, 10, 10);
                for(byte i=0;i<10;i++) Assert.AreEqual(i + 10, readData[i]);
            }
            var stream = msf.Stream;
            stream.AllowDispose = true;
            stream.Dispose();
        }

        [TestMethod]
        public void InitializeFile_ReadAt_Expects255Byte()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf))
            {
                var handle = file.Allocate();
                var data = new byte[255];
                for (byte i = 0; i < 255; i++) data[i] = i;
                file.Write(handle, data, 0, 255);

                var readData = new byte[512];
                var readBytes = file.ReadAt(handle, readData, 0, 512);
                for (byte i=0;i<255;i++) Assert.AreEqual(i, readData[i]);
                Assert.AreEqual((uint) 255, readBytes);
            }
            var stream = msf.Stream;
            stream.AllowDispose = true;
            stream.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void ReadAt_OffsetOutOfRange_ExpectsException()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf))
            {
                var handle = file.Allocate();
                var data = new byte[255];
                for (byte i = 0; i < 255; i++) data[i] = i;
                file.Write(handle, data, 0, 255);

                var readData = new byte[512];
                var readBytes = file.ReadAt(handle, readData, 512, 512);
                for (byte i = 0; i < 255; i++) Assert.AreEqual(i, readData[i]);
                Assert.AreEqual((uint)255, readBytes);
            }
            var stream = msf.Stream;
            stream.AllowDispose = true;
            stream.Dispose();            
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void ReadAt_WrongHandle_ExpectsException()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf))
            {
                var handle = file.Allocate();
                var data = new byte[255];
                for (byte i = 0; i < 255; i++) data[i] = i;
                file.Write(handle, data, 0, 255);

                var readData = new byte[512];
                var readBytes = file.ReadAt(handle + 1, readData, 0, 512);
                for (byte i = 0; i < 255; i++) Assert.AreEqual(i, readData[i]);
                Assert.AreEqual((uint)255, readBytes);
            }
            var stream = msf.Stream;
            stream.AllowDispose = true;
            stream.Dispose();                        
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ReadAt_BufferSmallerThanCount_ExpectsException()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf))
            {
                var handle = file.Allocate();
                var data = new byte[255];
                for (byte i = 0; i < 255; i++) data[i] = i;
                file.Write(handle, data, 0, 255);

                var readData = new byte[255];
                var readBytes = file.ReadAt(handle, readData, 0, 512);
                for (byte i = 0; i < 255; i++) Assert.AreEqual(i, readData[i]);
                Assert.AreEqual((uint)255, readBytes);
            }
            var stream = msf.Stream;
            stream.AllowDispose = true;
            stream.Dispose();                                    
        }

        [TestMethod]
        public void ReadAt_MultiplePages_AllData()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf))
            {
                var handle = file.Allocate();
                var data = new byte[8000];
                for (int i = 0; i < 8000; i++) data[i] = (byte) (i%255);
                file.Write(handle, data, 0, 8000);

                var readData = new byte[8000];
                var readBytes = file.ReadAt(handle, readData, 0, 8000);
                for (int i=0;i<8000;i++) Assert.AreEqual(data[i], readData[i]);
                Assert.AreEqual((uint)8000, readBytes);
            }
            var stream = msf.Stream;
            stream.AllowDispose = true;
            stream.Dispose();
        }

        [TestMethod]
        public void ReadAt_MultiplePages_NonZeroOffset()
        {
            const string path = "xxx";
            var msf = new UndisposableMemoryStreamFactory();
            using (var file = new CompoundFile(path, new CompoundFile.CompoundFileOptions(), msf))
            {
                var handle = file.Allocate();
                var data = new byte[8000];
                for (int i = 0; i < 8000; i++) data[i] = (byte)(i % 255);
                file.Write(handle, data, 0, 8000);

                var readData = new byte[4000];
                var readBytes = file.ReadAt(handle, readData, 2000, 4000);
                for (int i = 0; i < 4000; i++) Assert.AreEqual(data[i + 2000], readData[i]);
                Assert.AreEqual((uint)4000, readBytes);
            }
            var stream = msf.Stream;
            stream.AllowDispose = true;
            stream.Dispose();            
        }

        internal class UndisposableMemoryStreamFactory : IFileStreamFactory
        {
            public UndisposableMemoryStream Stream { get { return _ms; } }

            private UndisposableMemoryStream _ms;
            
            public string Path { get { return string.Empty; }}

            public UndisposableMemoryStreamFactory()
            {
                _ms = new UndisposableMemoryStream();
            }

            public Stream Create(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            {
                return _ms;
            }

            public Stream Create()
            {
                return _ms;
            }

           
        }

        internal class UndisposableMemoryStream : MemoryStream
        {
            public bool AllowDispose { get; set; }
            public UndisposableMemoryStream()
                : base() 
            {
                    AllowDispose = false;
            }

            protected override void Dispose(bool disposing)
            {
                if (AllowDispose)
                    base.Dispose(disposing);
            }
        }
    }
}
