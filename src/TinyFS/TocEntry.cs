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
using System;
using System.IO;

namespace TinyFS
{
    internal class TocEntry
    {
        public string Name { get; set; }
        public uint Length { get; set; }
        public uint Handle { get; set; }

        public TocEntry()
        {
            Name = string.Empty;
            Length = 0;
            Handle = 0;
        }

        public byte[] Serialize()
        {
            var ms = new MemoryStream();
            var namebuf = System.Text.Encoding.UTF8.GetBytes(Name);
            ms.Write(BitConverter.GetBytes(namebuf.Length), 0, sizeof(int));
            ms.Write(namebuf, 0, namebuf.Length);
            ms.Write(BitConverter.GetBytes(Length), 0, sizeof (uint));
            ms.Write(BitConverter.GetBytes(Handle), 0, sizeof(uint));
            ms.Flush();
            ms.Position = 0;
            return ms.ToArray();
        }

        public static TocEntry Create(byte[] item)
        {
            var ret = new TocEntry();
            var enc = new System.Text.UTF8Encoding();
            
            int ix = 0;
            var namelength = BitConverter.ToInt32(item, ix);
            ix += sizeof (Int32);
            ret.Name = enc.GetString(item, ix, namelength);
            ix += namelength;
            ret.Length = BitConverter.ToUInt32(item, ix);
            ix += sizeof (uint);
            ret.Handle = BitConverter.ToUInt32(item, ix);
            return ret;
        }

        public FileInfo ToFileInfo()
        {
            return new FileInfo(Name, Length, Handle);
        }
    }
}