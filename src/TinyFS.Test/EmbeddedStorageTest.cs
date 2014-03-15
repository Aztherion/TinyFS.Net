using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TinyFS.Test
{
    [TestClass]
    public class EmbeddedStorageTest
    {
        [TestMethod]
        public void InitializeEmptyFile_ExpectsNoFiles()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using(var es = new EmbeddedStorage(path))
            {
                var files = new List<FileInfo>(es.EnumerateFiles());
                Assert.AreEqual(0, files.Count);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void InitializeEmptyFile_AddOneFile()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var es = new EmbeddedStorage(path))
            {
                var fi = es.CreateFile("Q");
                Assert.AreEqual("Q", fi.Name);
                Assert.AreEqual((uint)0, fi.Length);
                var files = new List<FileInfo>(es.EnumerateFiles());
                Assert.AreEqual(1, files.Count);
                Assert.AreEqual(fi.Name, files[0].Name);
                Assert.AreEqual(fi.Length, files[0].Length);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void InitializeEmptyFile_WriteOneFile()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var es = new EmbeddedStorage(path))
            {
                var fi = es.CreateFile("Q");
                var data = new byte[255];
                for (byte i = 0; i < 255; i++) data[i] = i;
                es.Write(fi, data, 0, 255);
                var content = es.Read(fi);
                Assert.AreEqual(data.Length, content.Length);
                for(byte i=0;i<255;i++) Assert.AreEqual(data[i], content[i]);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void WriteOneFile_RemoveOneFile()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var es = new EmbeddedStorage(path))
            {
                var files = es.Files();
                Assert.AreEqual(0, files.Count);
                var fi = es.CreateFile("Q");
                var data = new byte[255];
                for (byte i = 0; i < 255; i++) data[i] = i;
                es.Write(fi, data, 0, 255);
                files = es.Files();
                Assert.AreEqual(1, files.Count);
                es.Remove(fi);
                files = es.Files();
                Assert.AreEqual(0, files.Count);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void ExistingFile_ContainsOneFile()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var es = new EmbeddedStorage(path))
            {
                var fi = es.CreateFile("Q");
            }
            using (var es = new EmbeddedStorage(path))
            {
                var files = es.Files();
                Assert.AreEqual(1, files.Count);
                Assert.AreEqual("Q", files[0].Name);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void ExistingFile_ReadBigFile()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            const int count = 8192;
            var data = new byte[count];
            for (var i = 0; i < count; i++) data[i] = (byte) (i%0xff);
            using (var es = new EmbeddedStorage(path))
            {
                var fi = es.CreateFile("Q");
                es.Write(fi, data, 0, count);
                var content = es.Read(fi);
                Assert.AreEqual(count, content.Length);
                for(var i=0;i<count;i++) Assert.AreEqual(data[i], content[i]);
            }
            if (File.Exists(path)) File.Delete(path);
        }

        [TestMethod]
        public void ExistingFile_SearchByFilename_ReadBigFile()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            const int count = 8192;
            var data = new byte[count];
            for (var i = 0; i < count; i++) data[i] = (byte)(i % 0xff);
            using (var es = new EmbeddedStorage(path))
            {
                var fi = es.CreateFile("Q");
                es.Write(fi, data, 0, count);
            }
            using (var es = new EmbeddedStorage(path))
            {
                var content = es.Read("Q");
                Assert.AreEqual(count, content.Length);
                for (var i = 0; i < count; i++) Assert.AreEqual(data[i], content[i]);
            }
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
