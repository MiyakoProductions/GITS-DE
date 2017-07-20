using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GITS_DE
{
    class DatArchive
    {
        private Stream _stream;

        public List<DatArchiveFile> Files = new List<DatArchiveFile>();

        public DatArchive(Stream stream)
        {
            this._stream = stream;
            DatArchiveFooter footer = new DatArchiveFooter(stream);
            stream.Position = (long)((ulong)footer.FileTableOffset);
            List<DatArchiveFileTableEntry> fileTableEntries = new List<DatArchiveFileTableEntry>();
            int i = 0;
            while ((long)i < (long)((ulong)footer.FileCount))
            {
                fileTableEntries.Add(new DatArchiveFileTableEntry(stream));
                i++;
            }
            foreach (DatArchiveFileTableEntry file in fileTableEntries)
            {
                stream.Position = (long)((ulong)file.FileOffset);
                this.Files.Add(new DatArchiveFile(stream, file.PackedSize));
            }
        }
    }
    public class DatArchiveFile
    {
        public const uint Magic = 576030941u;

        private Stream _stream;

        private long _offset;

        //private long _dataOffset;

        public string Name
        {
            get;
            private set;
        }

        public long _dataOffset
        {
            get;
            private set;
        }

        public uint Size
        {
            get;
            private set;
        }

        public DatArchiveFile(Stream stream, uint size)
        {
            this._stream = stream;
            this._offset = stream.Position;
            this.Size = size;
            BinaryReader br = new BinaryReader(stream);
            if (br.ReadUInt32() != 576030941u)
            {
                throw new Exception("Invalid file magic");
            }
            stream.Position += 22L;
            int fileNameLength = br.ReadInt32();
            this.Name = Encoding.ASCII.GetString(br.ReadBytes(fileNameLength));
            this._dataOffset = stream.Position;
        }

        public void Save(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] data = new byte[this.Size];
                this._stream.Position = this._dataOffset;
                this._stream.Read(data, 0, data.Length);
                fs.Write(data, 0, data.Length);
            }
        }
    }
    public class DatArchiveFileTableEntry
    {
        public const uint Magic = 707586951u;

        private Stream _stream;

        private long _offset;

        public uint PackedSize
        {
            get;
            private set;
        }

        public uint UnpackedSize
        {
            get;
            private set;
        }

        public string FileName
        {
            get;
            private set;
        }

        public uint FileOffset
        {
            get;
            private set;
        }

        public DatArchiveFileTableEntry(Stream stream)
        {
            this._stream = stream;
            this._offset = stream.Position;
            BinaryReader br = new BinaryReader(stream);
            if (br.ReadUInt32() != 707586951u)
            {
                throw new Exception("Invalid file table entry magic");
            }
            stream.Position += 16L;
            this.PackedSize = br.ReadUInt32();
            this.UnpackedSize = br.ReadUInt32();
            if (this.PackedSize != this.UnpackedSize)
            {
                throw new Exception("Temp assert");
            }
            int fileNameLength = br.ReadInt32();
            stream.Position += 10L;
            this.FileOffset = br.ReadUInt32();
            this.FileName = Encoding.ASCII.GetString(br.ReadBytes(fileNameLength));
        }
    }

    public class DatArchiveFooter
    {
        public const uint Magic = 862362094u;

        private Stream _stream;

        private long _offset;

        public uint FileCount
        {
            get;
            private set;
        }

        public uint FileTableOffset
        {
            get;
            private set;
        }

        public uint FileTableSize
        {
            get;
            private set;
        }

        public DatArchiveFooter(Stream stream)
        {
            this._stream = stream;
            BinaryReader br = new BinaryReader(stream);
            stream.Seek(-22L, SeekOrigin.End);
            this._offset = stream.Position;
            if (br.ReadUInt32() != 862362094u)
            {
                throw new Exception("Invalid footer magic");
            }
            if (br.ReadUInt32() != 0u)
            {
                throw new Exception("Assertion failure.");
            }
            ushort fileCount = br.ReadUInt16();
            ushort fileCount2 = br.ReadUInt16();
            if (fileCount != fileCount2)
            {
                throw new Exception("Assertion failure.");
            }
            this.FileCount = (uint)fileCount;
            this.FileTableSize = br.ReadUInt32();
            this.FileTableOffset = br.ReadUInt32();
            if (br.ReadUInt16() != 0)
            {
                throw new Exception("Assert failure");
            }
        }
    }

}
