using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FMS.FAT
{

  public class FileBase
  {
    public string name;
    public string path;
    public uint firstCluster;
    public FATDirectoryEntry entry;
    private Directory parent;

    public Directory Parent
    {
      set
      {
        if (value.path != null)
          path = value.path;
        if (value.name != null)
          path += value.name + "\\";

        parent = value;
      }
      get
      {
        return parent;
      }
    }

    public string FullName
    {
      get
      {
        return path + name;
      }
    }

    private IList<FATLongDirectoryEntry> longNameEntries;
    public void ProcessLongName(IList<FATLongDirectoryEntry> entries)
    {
      if (entries == null)
      {
        FormatName();
        return;
      }

      byte chksum = ComputeChecksum();
      foreach (var entry in entries)
      {
        if (entry.LDIR_Chksum != chksum)
        {
          FormatName();
          return;
        }
      }

      longNameEntries = entries;
      name = "";
      for (int i = longNameEntries.Count - 1; i >= 0; i--)
      {
        FATLongDirectoryEntry lng = longNameEntries[i];
        name += ConvertLongName(lng);
      }
    }

    private unsafe string ConvertLongName(FATLongDirectoryEntry lng)
    {
      char[] name = new char[13];
      fixed (char* pName = &name[0])
      {
        // Part 1:
        ConvertLongName(pName, lng.LDIR_Name1, 10, 13);
        // Part 2:
        ConvertLongName(pName + 5, lng.LDIR_Name2, 12, 8);
        // Part 3:
        ConvertLongName(pName + 11, lng.LDIR_Name3, 4, 2);
      }

      string str = new string(name);
      int index = str.IndexOf('\0');
      if (index >= 0)
        str = str.Remove(index);

      return str;
    }

    private unsafe void ConvertLongName(char* pName, byte* buffer, int bufferSize, int remainingSize)
    {
      Encoding.Unicode.GetChars(buffer, bufferSize, pName, remainingSize);
    }

    private byte ComputeChecksum()
    {
      byte chksum = 0;
      for (short i = 0; i < 11; i++)
      {
        byte oldChksum = chksum;
        chksum = (byte)(((chksum & (byte)1) != 0) ? (byte)0x80 : (byte)0);
        chksum += (byte)(oldChksum >> 1);
        chksum += (byte)name[i];
      }

      return (byte)chksum;
    }

    private void FormatName()
    {
      name = name.ToLower();
      string extension = name.Substring(name.Length - 3);
      string filename = name.Remove(name.Length - 3).Replace(" ", "");
      if (GetType() == typeof(Directory))
        name = filename;
      else
        name = filename + "." + extension;
    }
  }

  public class File : FileBase
  {
  }

  public class Directory: FileBase
  {
    public List<FileBase> children;

    public void Sort(DiskStructure diskStructure)
    {
      if (children == null)
        return;

      foreach (var child in children)
      {
        if (child.GetType() == typeof(Directory))
          ((Directory)child).Sort(diskStructure);
      }

      children.Sort(delegate(FileBase x, FileBase y)
      {
        int compare = 0;
        if (HandleDotAndDotDot(x, y, out compare))
          return compare;

        if ((x.GetType() == typeof(Directory)) &&
          (y.GetType() == typeof(File)))
          return 1; // Directory are after files

        if ((y.GetType() == typeof(Directory)) &&
          (x.GetType() == typeof(File)))
          return -1; // Directory are after files

        DiskStructure.Directory parent = diskStructure.Root.FindDirectory(x.path);
        Debug.Assert(parent == diskStructure.Root.FindDirectory(y.path));

        int xindex = (x.GetType() == typeof(Directory)) ? parent.GetIndexOfDirectory(x.name) : parent.GetIndexOfFile(x.name);
        int yindex = (y.GetType() == typeof(Directory)) ? parent.GetIndexOfDirectory(y.name) : parent.GetIndexOfFile(y.name);

        if (yindex < 0 || xindex < 0)
          throw new ArgumentOutOfRangeException();

        return (xindex - yindex);
      });
    }

    private bool HandleDotAndDotDot(FileBase x, FileBase y, out int compare)
    {
      if ((x.name == "." && y.name == ".") ||
         ((x.name == ".." && y.name == "..")))
      {
        compare = 0;
        return true;
      }

      if (x.name == ".")
      {
        compare = -1;
        return true;
      }

      if (y.name == ".")
      {
        compare = 1;
        return true;
      }

      if (x.name == "..")
      {
        if (y.name == ".")
        {
          compare = 1;
          return true;
        }
        else
        {
          compare = -1;
          return true;
        }
      }

      if (y.name == "..")
      {
        if (x.name == ".")
        {
          compare = -1;
          return true;
        }
        else
        {
          compare = 1;
          return true;
        }
      }

      compare = 0;
      return false;
    }
  }
  
  abstract class FAT
  {
    protected FATBS bootSector;
    protected BinaryReader reader;
    protected Stream stream;
    protected ILogger logger;

    public ulong rootDirSectors;
    public ulong firstDataSector;
    public ulong firstFatSector;
    public ulong dataSectors;
    public ulong totalClusters;
    public uint totalSectors;
    public uint tableSize;
    public uint mbrOffsetInByte;

    public Directory Root
    {
      get
      {
        return rootDirectory;
      }
    }
    protected Directory rootDirectory;
    private List<FATLongDirectoryEntry> longDirectoryBuffer;

    public FAT(string drive, FATBS bootSector, BinaryReader reader, Stream stream, ILogger logger)
    {
      this.bootSector = bootSector;
      this.reader = reader;
      this.stream = stream;
      this.logger = logger;
      rootDirectory = new Directory()
      {
        name = drive
      };
    }

    protected uint GetClusterSize()
    {
      return (uint) bootSector.sectors_per_cluster * bootSector.bytes_per_sector;
    }

    protected byte[] ReadCluster(uint cluster)
    {
      Debug.WriteLine("Reading cluster 0x{0:x8}", new object[] { cluster });
      SeekToCluster(cluster);
      return reader.ReadBytes((int) GetClusterSize());
    }

    protected void SeekToCluster(uint cluster)
    {
      ulong firstSector = ((cluster - 2) * bootSector.sectors_per_cluster) + firstDataSector;
      stream.Seek((long) (mbrOffsetInByte + firstSector * bootSector.bytes_per_sector), SeekOrigin.Begin);
    }

    protected void SeekToFATPosition(FATPosition position)
    {
      stream.Seek(mbrOffsetInByte + position.sector * bootSector.bytes_per_sector, SeekOrigin.Begin);
    }

    protected bool IsDirectoryFree(byte flag)
    {
      return flag == 0xE5;
    }

    protected bool IsDirectoryEOF(byte flag)
    {
      return flag == 0;
    }

    protected bool IsValidDirectoryEntry(byte flag)
    {
      return !IsDirectoryFree(flag) && !IsDirectoryEOF(flag);
    }

    public abstract void ReadRootDirectory();
    public FATPosition GetFATPositionForCluster(uint cluster)
    {
      uint fatOffset = GetFATEntrySize() * cluster;

      FATPosition position = new FATPosition();
      position.sector = bootSector.reserved_sector_count + (fatOffset / bootSector.bytes_per_sector);
      position.sector_offset = fatOffset % bootSector.bytes_per_sector;

      return position;
    }
    public abstract uint ReadFATEntry(FATPosition position);
    public abstract Boolean IsEOCFatEntry(uint entry);

    protected abstract uint GetFATEntrySize();

    protected List<FileBase> ReadStructure(uint cluster, Directory parent, bool recursive = true)
    {
      bool isEnded = false;
      uint currentCluster = cluster;
      List<FileBase> directoryEntries = new List<FileBase>();
      do
      {
        byte[] clusterData = ReadCluster(currentCluster);
        uint clusterSize = GetClusterSize();

        ReadDirectoriesInternal(clusterData, clusterSize, parent, directoryEntries);

        uint fatEntry = ReadFATEntry(GetFATPositionForCluster(currentCluster));
        isEnded = IsEOCFatEntry(fatEntry);
        if (!isEnded)
          currentCluster = fatEntry;

        if (isEnded)
          System.Diagnostics.Debug.WriteLine("This is the last cluster");
        else
          System.Diagnostics.Debug.WriteLine("There's a cluster after this one: {0}", new object[] { fatEntry });
      } while (!isEnded);

      if (recursive)
        ReadSubDirectories(directoryEntries);

      return directoryEntries;
    }

    protected void ReadSubDirectories(List<FileBase> directoryEntries)
    {
      foreach (var file in directoryEntries)
      {
        if (file.GetType() == typeof(Directory))
        {
          Directory dir = (Directory)file;
          if (dir.name == "." || dir.name == "..")
            continue;
          dir.children = ReadStructure(dir.firstCluster, dir);
        }
      }
    }

    protected void ReadDirectoriesInternal(byte[] data, uint size, Directory parent, List<FileBase> directoryEntries)
    {
      for (uint index = 0; index < size; index += (uint)Marshal.SizeOf(typeof(FATDirectoryEntry)))
      {
        unsafe
        {
          fixed (byte* entry = &data[index])
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
            file.Parent = parent;

            file.ProcessLongName(longDirectoryBuffer);
            longDirectoryBuffer = null;

            directoryEntries.Add(file);

            System.Diagnostics.Debug.WriteLine("File found: {0}, Attributes: {1}, Cluster: 0x{2:x8}, FileSize: {3}", new object[] { file.name, attributes, file.firstCluster, fatDirectoryEntry.DIR_FileSize.toFileSize() });
          }
        }
      }
    }

    protected void ReadFile(uint firstCluster, ulong length)
    {
      ulong red = 0;
      bool isEnded = false;
      uint currentCluster = firstCluster;
      byte[] data = new byte[length];
      do
      {
        byte[] clusterData = ReadCluster(currentCluster);
        uint clusterSize = GetClusterSize();

        ulong copyLength = clusterSize;
        if (length - red < clusterSize)
          copyLength = length - red;

        Array.Copy(clusterData, (long)0, data, (long)red, (long)copyLength);

        red += copyLength;

        uint fatEntry = ReadFATEntry(GetFATPositionForCluster(currentCluster));
        isEnded = IsEOCFatEntry(fatEntry);
        if (!isEnded)
          currentCluster = fatEntry;

        if (isEnded)
        {
          Debug.WriteLine("This is the last cluster");
          Debug.Assert(red == length);
        }
        else
          Debug.WriteLine("There's a cluster after this one: 0x{0:x8}", new object[] { fatEntry });
      } while (!isEnded);

      Debug.WriteLine("File content:\n{0}", new object[] { Utils.HexDump(data) });
    }

    protected void WriteDirectories(uint cluster, ICollection<FATDirectoryEntry> directories)
    {
      using (MemoryStream memoryStream = new MemoryStream())
      {
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {
          foreach (var directory in directories)
          {
            byte[] bytes = Utils.TypeToByte<FATDirectoryEntry>(directory);
            writer.Write(bytes);
          }
          writer.Write((byte)0);

          long toPad = bootSector.bytes_per_sector - memoryStream.Length % bootSector.bytes_per_sector;
          if (toPad > 0)
            writer.Write(new byte[toPad]);

          Debug.Assert(memoryStream.Length % bootSector.bytes_per_sector == 0);

          BinaryReader memoryReader = new BinaryReader(memoryStream);
          memoryStream.Seek(0, SeekOrigin.Begin);

          SeekToCluster(cluster);
          BinaryWriter diskWriter = new BinaryWriter(stream);
          diskWriter.Write(memoryReader.ReadBytes((int)memoryStream.Length));
          diskWriter.Flush();
        }
      }
    }

    public void Sort(DiskStructure diskStructure)
    {
      rootDirectory.Sort(diskStructure);
    }
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
  public unsafe struct FAT32ExtBS
  {
    //extended fat32 stuff
    public uint table_size_32;
    public ushort extended_flags;
    public short fat_version;
    public uint root_cluster;
    public ushort fat_info;
    public ushort backup_BS_sector;
    public fixed byte reserved_0[12];
    public byte drive_number;
    public byte reserved_1;
    public byte boot_signature;
    public uint volume_id;
    public fixed byte volume_label[11];
    public fixed byte fat_type_label[8];
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
  public struct FAT16ExtBS
  {
    //extended fat12 and fat16 stuff
    public byte bios_drive_num;
    public byte reserved1;
    public byte boot_signature;
    public uint volume_id;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
    public string volume_label;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
    public string fat_type_label;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
  public unsafe struct FATBS
  {
    public fixed byte bootjmp[3];
    public fixed byte oem_name[8];
    public ushort bytes_per_sector;
    public byte sectors_per_cluster;
    public ushort reserved_sector_count;
    public byte table_count;
    public ushort root_entry_count;
    public ushort total_sectors_16;
    public byte media_type;
    public ushort table_size_16;
    public ushort sectors_per_track;
    public ushort head_side_count;
    public uint hidden_sector_count;
    public uint total_sectors_32;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
  public unsafe struct FATDirectoryEntry
  {
    public fixed byte DIR_Name[11];
    public byte DIR_Attr;
    public byte DIR_NTRes;
    public byte DIR_CrtTimeTenth;
    public ushort DIR_CrtTime;
    public ushort DIR_CrtDate;
    public ushort DIR_LstAccData;
    public ushort DIR_FstClusHI;
    public ushort DIR_WrtTime;
    public ushort DIR_WrtData;
    public ushort DIR_FstClusLO;
    public uint DIR_FileSize;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
  public unsafe struct FATLongDirectoryEntry
  {
    public byte LDIR_Ord;
    public fixed byte LDIR_Name1[10];
    public byte LDIR_Attr;
    public byte LDIR_Type;
    public byte LDIR_Chksum;
    public fixed byte LDIR_Name2[12];
    public ushort LDIR_FstClusLO;
    public fixed byte LDIR_Name3[4];
  }

  public struct FATPosition
  {
    public uint sector;
    public uint sector_offset;
  }

  [Flags]
  public enum DirectoryAttributes
  {
    ReadOnly = 0x01,
    Hidden = 0x02,
    System = 0x04,
    VolumeId = 0x08,
    Directory = 0x10,
    Archive = 0x20,
    LongName = ReadOnly | Hidden | System | VolumeId
  }
}
