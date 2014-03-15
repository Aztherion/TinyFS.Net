using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TinyFS.Test
{
    [TestClass]
    public class EmbeddedStorageStreamTest
    {
        [TestMethod]
        public void InitializeEmptyFile_GetLength()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path)) File.Delete(path);
            using (var es = new EmbeddedStorage(path))
            {
                var ess = new EmbeddedStorageStream("Q", es);
                var length = ess.Length;
                Assert.AreEqual(0, length);
                var files = es.Files();
                Assert.AreEqual(1, files.Count);
                Assert.AreEqual("Q", files[0].Name);
            }
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
