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
using System.Collections.Generic;
using System.IO;

namespace TinyFS
{
    internal class Toc
    {
        private const byte TOC_MAGIC_1 = 0x01;
        private const byte TOC_MAGIC_2 = 0x02;
        private const byte TOC_MAGIC_3 = 0x03;
        private const byte TOC_MAGIC_4 = 0x04;

        public List<TocEntry> Entries { get; private set; }

        public Toc()
        {
            Entries = new List<TocEntry>();
        }

        public bool Deserialize(byte[] toc)
        {
            if (toc[0] != TOC_MAGIC_1) return false;
            if (toc[1] != TOC_MAGIC_2) return false;
            if (toc[2] != TOC_MAGIC_3) return false;
            if (toc[3] != TOC_MAGIC_4) return false;

            int ix = 4;
            while(true)
            {
                var count = BitConverter.ToInt32(toc, ix);
                if (count == 0) break;
                ix += sizeof (Int32);
                if (ix + count > toc.Length) break;
                var item = new byte[count];
                Buffer.BlockCopy(toc, ix, item, 0, count); 
                Entries.Add(TocEntry.Create(item));
                ix += count;
            }
            return true;
        }

        public byte[] Serialize()
        {
            var ms = new MemoryStream();
            ms.WriteByte(TOC_MAGIC_1);
            ms.WriteByte(TOC_MAGIC_2);
            ms.WriteByte(TOC_MAGIC_3);
            ms.WriteByte(TOC_MAGIC_4);
            foreach(var entry in Entries)
            {
                var buf = entry.Serialize();
                ms.Write(BitConverter.GetBytes(buf.Length), 0, sizeof (int));
                ms.Write(buf, 0, buf.Length);
            }
            // empty uint32
            ms.WriteByte(0x0);
            ms.WriteByte(0x0);
            ms.WriteByte(0x0);
            ms.WriteByte(0x0);

            ms.Flush();
            ms.Position = 0;
            return ms.ToArray();
        }
    }
}