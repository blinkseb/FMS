using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace FMS.FAT.Implementation
{
  class FAT16: FAT
  {
    private FAT16ExtBS fat16ExtBS;
    private List<FATLongDirectoryEntry> longDirectoryBuffer;

    public FAT16(string drive, FATBS bootSector, FAT16ExtBS extSector, BinaryReader reader, Stream stream, ILogger logger)
      : base(drive, bootSector, reader, stream, logger)
    {
      this.fat16ExtBS = extSector;
    }

    public override void ReadRootDirectory()
    {
      ulong firstRootDirSector = (ulong) (bootSector.reserved_sector_count + (bootSector.table_count * bootSector.table_size_16)) * bootSector.bytes_per_sector;
      stream.Seek((long) firstRootDirSector, SeekOrigin.Begin);

      int sizeOfRootDirectory = bootSector.root_entry_count * Marshal.SizeOf(typeof(FATDirectoryEntry));

      List<FileBase> directoryEntries = new List<FileBase>();

      byte[] clusterData = reader.ReadBytes(sizeOfRootDirectory);
      uint index = 0;
      uint clusterSize = GetClusterSize();
      for (; index < sizeOfRootDirectory; index += (uint)Marshal.SizeOf(typeof(FATDirectoryEntry)))
      {
        unsafe
        {
          fixed (byte* entry = &clusterData[index])
          {
            FATDirectoryEntry fatDirectoryEntry = Utils.Create<FATDirectoryEntry>(entry);
            byte flag = fatDirectoryEntry.DIR_Name[0];
            if (IsDirectoryEOF(flag))
              break;
            if (IsDirectoryFree(flag))
              continue;

            DirectoryAttributes attributes = (DirectoryAttributes)fatDirectoryEntry.DIR_Attr;
            if (attributes == DirectoryAttributes.LongName)
            {
              FATLongDirectoryEntry longEntry = Utils.Create<FATLongDirectoryEntry>(entry);

              // Ok it's weird, but the last one is the first one...
              bool isLast = (longEntry.LDIR_Ord & 0x40) == 0x40;
              if (!isLast && longDirectoryBuffer == null)
                continue;

              if (isLast)
                longDirectoryBuffer = new List<FATLongDirectoryEntry>();

              longDirectoryBuffer.Add(longEntry);

              continue;
            }

            FileBase file;
            if (attributes.HasFlag(DirectoryAttributes.Directory))
              file = new Directory();
            else
              file = new File();

            file.name = Utils.ByteToString(fatDirectoryEntry.DIR_Name, 11);
            file.firstCluster = (uint)(fatDirectoryEntry.DIR_FstClusHI << 16 | fatDirectoryEntry.DIR_FstClusLO & 0xFFFF);
            file.entry = fatDirectoryEntry;
            file.Parent = rootDirectory;

            file.ProcessLongName(longDirectoryBuffer);
            longDirectoryBuffer = null;

            directoryEntries.Add(file);

            System.Diagnostics.Debug.WriteLine("File found: {0}, Attributes: {1}, Cluster: 0x{2:x8}, FileSize: {3}", new object[] { file.name, attributes, file.firstCluster, fatDirectoryEntry.DIR_FileSize.toFileSize() });
          }
        }
      }

      if (true)
      {
        foreach (var file in directoryEntries)
        {
          if (file.GetType() == typeof(Directory))
          {
            Directory dir = (Directory)file;
            if (dir.name[0] == '.')
              continue;
            dir.children = ReadStructure(dir.firstCluster, dir);
          }
        }
      }

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
