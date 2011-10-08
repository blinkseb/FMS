/*
 * This file is part of the FMS project
 *
 * (c) 2011 Sébastien Brochet <blinkseb-nospam-@madalynn.eu>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace FMS.FAT.Implementation
{
  class FAT16: FAT
  {
    private FAT16ExtBS fat16ExtBS;

    public FAT16(string drive, FATBS bootSector, FAT16ExtBS extSector, BinaryReader reader, Stream stream, ILogger logger)
      : base(drive, bootSector, reader, stream, logger)
    {
      this.fat16ExtBS = extSector;
    }

    public override ulong GetRootDirectorySector()
    {
      return (ulong)(bootSector.reserved_sector_count + (bootSector.table_count * bootSector.table_size_16));
    }

    public override void ReadRootDirectory()
    {
      ulong firstRootDirSector = GetRootDirectorySector() * bootSector.bytes_per_sector;
      stream.Seek((long) firstRootDirSector, SeekOrigin.Begin);

      uint sizeOfRootDirectory = (uint) (bootSector.root_entry_count * Marshal.SizeOf(typeof(FATDirectoryEntry)));

      List<FileBase> directoryEntries = new List<FileBase>();

      byte[] data = reader.ReadBytes((int) sizeOfRootDirectory);

      ReadDirectoriesInternal(data, sizeOfRootDirectory, rootDirectory, directoryEntries);
      ReadSubDirectories(directoryEntries);

      rootDirectory.children = directoryEntries;
    }

    protected override uint GetFATEntrySize()
    {
      return 2;
    }

    public override uint ReadFATEntry(FATPosition position)
    {
      SeekToFATPosition(position);
      byte[] sector = reader.ReadBytes(bootSector.bytes_per_sector);
      return BitConverter.ToUInt16(sector, (int)position.sector_offset);
    }

    public override bool IsEOCFatEntry(uint entry)
    {
      return ((ushort)(entry & 0x0000FFFF)) > 0xFFF8;
    }
  }
}
