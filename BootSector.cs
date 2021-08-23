using System;
using System.Collections.Generic;
using System.Text;
using DiscUtils.Streams;
using DiscUtils.Fat;
namespace Q9xS
{
    class BootSector
    {

        /// <summary>
        /// Gets the OEM name from the file system.
        /// </summary>
        public string OemName { get; private set; }


        /// <summary>
        /// Gets the number of contiguous sectors that make up one cluster.
        /// </summary>
        public byte SectorsPerCluster { get; private set; }

        private byte[] _bootSector;
        private ushort _bpbBkBootSec;

        private ushort _bpbBytesPerSec;
        private ushort _bpbExtFlags;
        private ushort _bpbFATSz16;

        private uint _bpbFATSz32;
        private ushort _bpbFSInfo;
        private ushort _bpbFSVer;
        private uint _bpbHiddSec;
        private ushort _bpbNumHeads;
        private uint _bpbRootClus;
        private ushort _bpbRootEntCnt;
        private ushort _bpbRsvdSecCnt;
        private ushort _bpbSecPerTrk;
        private ushort _bpbTotSec16;
        private uint _bpbTotSec32;

        private byte _bsBootSig;
        private uint _bsVolId;
        private string _bsVolLab;


        /// <summary>
        /// Gets the number of FATs present.
        /// </summary>
        public byte FatCount { get; private set; }

        /// <summary>
        /// Gets the Media marker byte, which indicates fixed or removable media.
        /// </summary>
        public byte Media { get; private set; }

        /// <summary>
        /// Gets the FAT variant of the file system.
        /// </summary>
        public FatType FatVariant { get; private set; }

        /// <summary>
        /// Gets the BIOS drive number for BIOS Int 13h calls.
        /// </summary>
        public byte BiosDriveNumber { get; private set; }

        /// <summary>
        /// Gets the (informational only) file system type recorded in the meta-data.
        /// </summary>
        public string FileSystemType { get; private set; }

        /// <summary>
        /// Gets the size of a single FAT, in sectors.
        /// </summary>
        public long FatSize
        {
            get { return _bpbFATSz16 != 0 ? _bpbFATSz16 : _bpbFATSz32; }
        }

        /// <summary>
        /// Gets the sector location of the FSINFO structure (FAT32 only).
        /// </summary>
        public int FSInfoSector
        {
            get { return _bpbFSInfo; }
        }

        /// <summary>
        /// Gets the number of logical heads.
        /// </summary>
        public int Heads
        {
            get { return _bpbNumHeads; }
        }

        /// <summary>
        /// Gets the number of hidden sectors, hiding partition tables, etc.
        /// </summary>
        public long HiddenSectors
        {
            get { return _bpbHiddSec; }
        }

        /// <summary>
        /// Gets the maximum number of root directory entries (on FAT variants that have a limit).
        /// </summary>
        public int MaxRootDirectoryEntries
        {
            get { return _bpbRootEntCnt; }
        }
        /// <summary>
        /// Gets the active FAT (zero-based index).
        /// </summary>
        public byte ActiveFat
        {
            get { return (byte)((_bpbExtFlags & 0x08) != 0 ? _bpbExtFlags & 0x7 : 0); }
        }

        /// <summary>
        /// Gets the Sector location of the backup boot sector (FAT32 only).
        /// </summary>
        public int BackupBootSector
        {
            get { return _bpbBkBootSec; }
        }


        private void ReadBS(byte[] bootSector, int offset)
        {
            BiosDriveNumber = bootSector[offset];
            _bsBootSig = bootSector[offset + 2];
            _bsVolId = EndianUtilities.ToUInt32LittleEndian(bootSector, offset + 3);
            _bsVolLab = Encoding.ASCII.GetString(bootSector, offset + 7, 11);
            FileSystemType = Encoding.ASCII.GetString(bootSector, offset + 18, 8);
        }

        private void ReadBPB(byte[] bootSector)
        {
            if (bootSector.Length != 512)
            {
                throw new Exception("NO!");
            }
            OemName = Encoding.ASCII.GetString(_bootSector, 3, 8).TrimEnd('\0');
            _bpbBytesPerSec = EndianUtilities.ToUInt16LittleEndian(_bootSector, 11);
            SectorsPerCluster = _bootSector[13];
            _bpbRsvdSecCnt = EndianUtilities.ToUInt16LittleEndian(_bootSector, 14);
            FatCount = _bootSector[16];
            _bpbRootEntCnt = EndianUtilities.ToUInt16LittleEndian(_bootSector, 17);
            _bpbTotSec16 = EndianUtilities.ToUInt16LittleEndian(_bootSector, 19);
            Media = _bootSector[21];
            _bpbFATSz16 = EndianUtilities.ToUInt16LittleEndian(_bootSector, 22);
            _bpbSecPerTrk = EndianUtilities.ToUInt16LittleEndian(_bootSector, 24);
            _bpbNumHeads = EndianUtilities.ToUInt16LittleEndian(_bootSector, 26);
            _bpbHiddSec = EndianUtilities.ToUInt32LittleEndian(_bootSector, 28);
            _bpbTotSec32 = EndianUtilities.ToUInt32LittleEndian(_bootSector, 32);

            if (FatVariant != FatType.Fat32)
            {
                ReadBS(bootSector, 36);
            }
            else
            {
                _bpbFATSz32 = EndianUtilities.ToUInt32LittleEndian(_bootSector, 36);
                _bpbExtFlags = EndianUtilities.ToUInt16LittleEndian(_bootSector, 40);
                _bpbFSVer = EndianUtilities.ToUInt16LittleEndian(_bootSector, 42);
                _bpbRootClus = EndianUtilities.ToUInt32LittleEndian(_bootSector, 44);
                _bpbFSInfo = EndianUtilities.ToUInt16LittleEndian(_bootSector, 48);
                _bpbBkBootSec = EndianUtilities.ToUInt16LittleEndian(_bootSector, 50);
                ReadBS(bootSector, 64);
            }
        }
    }
}
