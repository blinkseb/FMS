/*
 * This file is part of the FMS project
 *
 * (c) 2011 Sébastien Brochet <blinkseb-nospam-@madalynn.eu>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 */

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.IO;
using FMS.FAT.Implementation;

namespace FMS.FAT
{

  public enum Type
  {
    Undetermined,
    FAT12,
    FAT16,
    FAT32
  }

  class FATReader
  {
    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern SafeFileHandle CreateFile(
        string fileName,
        [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
        int securityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes fileAttributes,
        IntPtr template);

    public const int FSCTL_DISMOUNT_VOLUME = 0x00090020;
    public const int FSCTL_LOCK_VOLUME     = 0x00090018;

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool DeviceIoControl(
      SafeFileHandle hDevice,
      uint dwIoControlCode,
      IntPtr lpInBuffer,
      uint nInBufferSize,
      IntPtr lpOutBuffer,
      uint nOutBufferSize,
      out uint lpBytesReturned,
      IntPtr lpOverlapped);

    private string mFileName;
    private SafeFileHandle mDiskHandle;
    private FileStream InStream;
    private BinaryReader binaryReader;

    private FATBS bootSector;
    private FAT16ExtBS fat16ExtBS;
    private FAT32ExtBS fat32ExtBS;

    public Type type = Type.Undetermined;

    private FMS.FAT.FAT fatImplementation;
    private ILogger logger;

    public FATReader(string filename, ILogger logger)
    {
      mFileName = Path.GetPathRoot(filename).Remove(2);
      this.logger = logger;
    }

    public unsafe void Open(FileAccess desiredAccess)
    {
      string fileName = String.Format(@"\\.\{0}", mFileName);

      mDiskHandle = CreateFile(fileName,
            desiredAccess,
            FileShare.ReadWrite, // drives must be opened with read and write share access
            0,
            FileMode.Open,
            FileAttributes.Normal,
            IntPtr.Zero);

      if (mDiskHandle.IsInvalid)
          throw new Win32Exception(Marshal.GetLastWin32Error());

      if (desiredAccess.HasFlag(FileAccess.Write))
      {
        // Unmount & lock the drive
        uint pByteReturned = 0;
        bool ret = DeviceIoControl(mDiskHandle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out pByteReturned, IntPtr.Zero);

        if (!ret)
          throw new Win32Exception(Marshal.GetLastWin32Error());

        ret = DeviceIoControl(mDiskHandle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out pByteReturned, IntPtr.Zero);
        if (!ret)
          throw new Win32Exception(Marshal.GetLastWin32Error());
      }

      InStream = new FileStream(mDiskHandle, desiredAccess);
      {
        binaryReader = new BinaryReader(InStream);
        {
          bootSector = Utils.ByteToType<FATBS>(binaryReader);
          //TODO: Check jmpSector for valid FAT

          ulong rootDirSectors = (ulong)((bootSector.root_entry_count * 32) + (bootSector.bytes_per_sector - 1)) / bootSector.bytes_per_sector;

          if (rootDirSectors == 0)
            type = Type.FAT32;

          if (type == Type.FAT32)
            fat32ExtBS = Utils.ByteToType<FAT32ExtBS>(binaryReader);
          else
            fat16ExtBS = Utils.ByteToType<FAT16ExtBS>(binaryReader);

          uint tableSize = 0;
          if (bootSector.table_size_16 != 0)
            tableSize = bootSector.table_size_16;
          else
            tableSize = fat32ExtBS.table_size_32;

          ulong firstDataSector = (ulong)(bootSector.reserved_sector_count + (bootSector.table_count * tableSize) + rootDirSectors);

          uint totalSectors;
          if (bootSector.total_sectors_16 != 0)
            totalSectors = bootSector.total_sectors_16;
          else
            totalSectors = bootSector.total_sectors_32;

          ulong dataSectors = (ulong)totalSectors - (bootSector.reserved_sector_count + (bootSector.table_count * tableSize) + rootDirSectors);

          ulong totalClusters = (ulong)(dataSectors / bootSector.sectors_per_cluster);

          if (totalClusters < 4085)
            type = Type.FAT12;
          else if (totalClusters < 65525)
            type = Type.FAT16;
          else
            type = Type.FAT32;

          if (type == Type.FAT12)
            throw new NotSupportedException("FAT12 is not supported");

          logger.Log("Detected new partition: {0}", new object[] { type });

          if (type == Type.FAT16)
            fatImplementation = new FAT16(mFileName, bootSector, fat16ExtBS, binaryReader, InStream, logger);
          else
            fatImplementation = new FAT32(mFileName, bootSector, fat32ExtBS, binaryReader, InStream, logger);

          fatImplementation.rootDirSectors = rootDirSectors;
          fatImplementation.tableSize = tableSize;
          fatImplementation.totalClusters = totalClusters;
          fatImplementation.totalSectors = totalSectors;
          fatImplementation.dataSectors = dataSectors;
          fatImplementation.firstDataSector = firstDataSector;
          fatImplementation.mbrOffsetInByte = 0;

          logger.Log("Table size: {0}", new object[] { tableSize });
          logger.Log("Clusters: {0}", new object[] { totalClusters });
          logger.Log("Sectors: {0}", new object[] { totalSectors });
          logger.Log("");
          logger.Log("Reading FAT...");

          fatImplementation.ReadRootDirectory();

          logger.Log("Reading done.");
        }
      }
    }

    public void Close()
    {
      if (InStream != null)
        InStream.Close();

      if (! mDiskHandle.IsClosed)
        mDiskHandle.Close();
    }

    public void Sort(DiskStructure diskStructure)
    {
      fatImplementation.Sort(diskStructure);
    }

    public void DumpFAT()
    {
      if (fatImplementation != null)
        Utils.DumpStructure(fatImplementation.Root.children, logger);
    }

    public void WriteFAT()
    {
      if (fatImplementation != null)
        fatImplementation.WriteFAT();
    }
  }
}
