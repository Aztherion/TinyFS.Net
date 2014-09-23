#region Copyright 2014 by Benny Olsson, benny@unitednerds.se, Licensed under the Apache License, Version 2.0
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
using System.IO;

namespace TinyFS
{
    internal class FileStreamFactory : IFileStreamFactory
    {
        private readonly Options _options;

        public string Path { get { return _options.Filename; } }

        public FileStreamFactory(Options options)
        {
            _options = options;
        }

        public Stream Create()
        {
            return new FileStream(_options.Filename, _options.FileMode, _options.FileAccess, _options.FileShare, _options.BufferSize, _options.FileOptions);
        }

        public Stream Create(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            return new FileStream(path, mode, access, share, bufferSize, options);
        }

        public class Options
        {
            public int BufferSize { get; private set; }
            public string Filename { get; private set; }
            public FileMode FileMode { get; private set; }
            public FileAccess FileAccess { get; private set; }
            public FileShare FileShare { get; private set; }
            public FileOptions FileOptions { get; private set; }

            private const int DEFAULT_BUFFER = 4096;

            public Options(string filename, FileMode filemode)
                : this(filename, filemode, filemode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite) { }

            public Options(string filename, FileMode filemode, FileAccess fileaccess)
                : this(filename, filemode, fileaccess, FileShare.None) { }

            public Options(string filename, FileMode filemode, FileAccess fileaccess, FileShare fileshare)
                : this(filename, filemode, fileaccess, fileshare, FileOptions.None) { }

            public Options(string filename, FileMode filemode, FileAccess fileaccess, FileShare fileshare, FileOptions fileoptions)
                : this(filename, filemode, fileaccess, fileshare, fileoptions, DEFAULT_BUFFER) { }

            public Options(string filename, FileMode filemode, FileAccess fileaccess, FileShare fileshare, FileOptions fileoptions, int bufferSize)
            {
                BufferSize = bufferSize;
                Filename = filename;
                FileMode = filemode;
                FileAccess = fileaccess;
                FileShare = fileshare;
                FileOptions = fileoptions;
            }
        }
    }
}
