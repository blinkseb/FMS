using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections;

namespace FMS.FAT.Implementation
{
  class FAT32: FAT
  {
    private FAT32ExtBS fat32ExtBS;

    public FAT32(string drive, FATBS bootSector, FAT32ExtBS extSector, BinaryReader reader, Stream stream, ILogger logger)
      : base(drive, bootSector, reader, stream, logger)
    {
      this.fat32ExtBS = extSector;
    }

    public override void ReadRootDirectory()
    {
      rootDirectory.children = ReadStructure(fat32ExtBS.root_cluster, rootDirectory);
    }

    protected override uint GetFATEntrySize()
    {
      return 4;
    }

    public override uint ReadFATEntry(FATPosition position)
    {
      SeekToFATPosition(position);
      byte[] sector = reader.ReadBytes(bootSector.bytes_per_sector);
      return BitConverter.ToUInt32(sector, (int) position.sector_offset) & 0x0FFFFFFF;
    }

    public override bool IsEOCFatEntry(uint entry)
    {
      return entry > 0x0FFFFFF8;
    }
  }
}
