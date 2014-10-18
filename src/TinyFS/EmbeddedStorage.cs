﻿#region Copyright 2014 by Benny Olsson, benny@unitednerds.se, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#region todos
// todo         write tests for EmbeddedStorageStream
#endregion
namespace TinyFS
{
    public class EmbeddedStorage : IDisposable
    {
        private CompoundFile _compoundFile;
        private readonly Toc _toc;
        private const uint TOC_HANDLE = 1;
        private bool _disposed;
        private readonly object _sync = new object();

        public EmbeddedStorage(string path)
        {
            _compoundFile = new CompoundFile(path, new CompoundFile.CompoundFileOptions{ FlushAtWrite = true, UseWriteCache = false });
            _toc = new Toc();
            
            if (InitializeToc()) return;
            
            if (!AllocateToc())
            {
                throw new Exception("Catastrophic failure. Could not allocate TOC. File '" + path + "'");
            }
        }

        ~EmbeddedStorage() { Dispose(false); }

        public IEnumerable<FileInfo> EnumerateFiles()
        {
            lock(_sync)
                return _toc.Entries.Select(entry => entry.ToFileInfo());
        }

        public List<FileInfo> Files()
        {
            lock(_sync)
                return _toc.Entries.Select(entry => entry.ToFileInfo()).ToList();
        }

        public FileInfo CreateFile(string filename)
        {
            lock(_sync)
            {
                if (_toc.Entries.Any(t => t.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase))) throw new IOException("file exists");
                var entry = new TocEntry { Handle = _compoundFile.Allocate(), Name = filename, Length = 0 };
                _toc.Entries.Add(entry);
                WriteToc();
                return entry.ToFileInfo();                
            }
        }

        public bool Exists(string filename)
        {
            lock (_sync)
                return _toc.Entries.Any(t => t.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase));
        }

        public byte[] Read(string filename)
        {
            uint handle;
            lock (_sync)
            {
                if (!_toc.Entries.Any(t => t.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase))) throw new FileNotFoundException();
                handle = _toc.Entries.Find(t => t.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase)).Handle;
            }
            return Read(handle);
        }

        public byte[] Read(FileInfo fileInfo)
        {
            uint handle;
            lock(_sync)
            {
                if (!_toc.Entries.Any(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase))) throw new FileNotFoundException();
                var entry = _toc.Entries.Find(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase));
                handle = entry.Handle;
            }
            return Read(handle);
        }

        public uint ReadAt(FileInfo fileInfo, byte[] buffer, uint srcOffset, uint count)
        {
            uint handle;
            lock(_sync)
            {
                if (!_toc.Entries.Any(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase))) throw new FileNotFoundException();
                var entry = _toc.Entries.Find(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase));
                handle = entry.Handle;
            }
            return ReadAt(handle, buffer, srcOffset, count);
        }

        public void Write(string filename, byte[] buffer, int offset, int count)
        {
            lock(_sync)
            {
                if (!_toc.Entries.Any(t => t.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase))) throw new FileNotFoundException();
                Write(_toc.Entries.Find(t => t.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase)).ToFileInfo(), buffer, offset, count);                
            }
        }

        public void Write(FileInfo fileInfo, byte[] buffer, int offset, int count)
        {
            uint handle;
            lock (_sync)
            {
                if (!_toc.Entries.Any(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase)))
                    throw new FileNotFoundException();
                var entry = _toc.Entries.Find(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase));
                handle = entry.Handle;
            }

            _compoundFile.Write(handle, buffer, offset, count);

            lock(_sync){
                _toc.Entries.Find(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase)).Length = (uint)count;
                WriteToc();                
            }
        }

        public void WriteAt(FileInfo fileInfo, byte[] buffer, int offset, int count, uint destOffset)
        {
            uint handle;
            lock (_sync)
            {
                if (!_toc.Entries.Any(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase)))
                    throw new FileNotFoundException();
                var entry = _toc.Entries.Find(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase));
                handle = entry.Handle;
            }
            _compoundFile.WriteAt(handle, destOffset, buffer, offset, count);
            var totalLength = _compoundFile.GetLength(handle);            
            lock (_sync)
            {
                _toc.Entries.Find(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase)).Length = totalLength;
                WriteToc();                
            }
        }

        public void Remove(string filename)
        {
            lock(_sync)
            {
                if (!_toc.Entries.Any(t => t.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase))) throw new FileNotFoundException();
                Remove(_toc.Entries.Find(t => t.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase)).ToFileInfo());                
            }
        }

        public void Remove(FileInfo fileInfo)
        {
            uint handle;
            lock(_sync)
            {
                if (!_toc.Entries.Any(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase))) throw new FileNotFoundException();
                var entry = _toc.Entries.Find(t => t.Name.Equals(fileInfo.Name, StringComparison.InvariantCultureIgnoreCase));
                handle = entry.Handle;
                _toc.Entries.Remove(entry);
                WriteToc();                                
            }
            _compoundFile.Free(handle);
        }

        private bool InitializeToc()
        {
            try
            {
                var toc = _compoundFile.ReadAll(TOC_HANDLE);
                _toc.Deserialize(toc);
                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        private bool AllocateToc()
        {
            try
            {
                var handle = _compoundFile.Allocate();
                if (handle != TOC_HANDLE) return false;
                WriteToc();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        private void WriteToc()
        {
            var toc = _toc.Serialize();
            _compoundFile.Write(TOC_HANDLE, toc, 0, toc.Length);            
        }

        private byte[] Read(uint handle)
        {
            return _compoundFile.ReadAll(handle);
        }

        private uint ReadAt(uint handle, byte[] buffer, uint srcOffset, uint count)
        {
            return _compoundFile.ReadAt(handle, buffer, srcOffset, count);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_disposed) return;
            _disposed = true;
            if (_compoundFile != null)
            {
                lock (_sync)
                {
                    _compoundFile.Dispose();
                    _compoundFile = null;
                }                
            }
        }
    }
}
