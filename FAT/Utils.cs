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
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace FMS.FAT
{
  class Utils
  {
    public static T ByteToType<T>(BinaryReader reader)
    {
      byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
      return ByteToType<T>(bytes);
    }

    public static T ByteToType<T>(byte[] bytes)
    {
      GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
      T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
      handle.Free();

      return theStructure;
    }

    public static byte[] TypeToByte<T>(T type)
    {
      GCHandle handle = GCHandle.Alloc(type, GCHandleType.Pinned);
      Marshal.StructureToPtr(type, handle.AddrOfPinnedObject(), true);
      
      var sizeInBytes = Marshal.SizeOf(typeof(T));
      byte[] bytes = new byte[sizeInBytes];

      Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, sizeInBytes);

      return bytes;
    }

    public unsafe static T[] Create<T>(void* source, int length)
    {
      var type = typeof(T);
      var sizeInBytes = Marshal.SizeOf(typeof(T));

      T[] output = new T[length];

      if (type.IsPrimitive)
      {
        // Make sure the array won't be moved around by the GC 
        var handle = GCHandle.Alloc(output, GCHandleType.Pinned);

        var destination = (byte*)handle.AddrOfPinnedObject().ToPointer();
        var byteLength = length * sizeInBytes;

        // There are faster ways to do this, particularly by using wider types or by 
        // handling special lengths.
        for (int i = 0; i < byteLength; i++)
          destination[i] = ((byte*)source)[i];

        handle.Free();
      }
      else if (type.IsValueType)
      {
        if (!type.IsLayoutSequential && !type.IsExplicitLayout)
        {
          throw new InvalidOperationException(string.Format("{0} does not define a StructLayout attribute", type));
        }

        IntPtr sourcePtr = new IntPtr(source);

        for (int i = 0; i < length; i++)
        {
          IntPtr p = new IntPtr((byte*)source + i * sizeInBytes);

          output[i] = (T)System.Runtime.InteropServices.Marshal.PtrToStructure(p, typeof(T));
        }
      }
      else
      {
        throw new InvalidOperationException(string.Format("{0} is not supported", type));
      }

      return output;
    }

    public unsafe static T Create<T>(void* source)
    {
      var type = typeof(T);
      var sizeInBytes = Marshal.SizeOf(typeof(T));
      //var sizeInBytes = sizeof(T);

      if (type.IsPrimitive)
      {
        byte[] bytes = Create<byte>(source, sizeInBytes);
        return ByteToType<T>(bytes);
      }
      else if (type.IsValueType)
      {
        if (!type.IsLayoutSequential && !type.IsExplicitLayout)
        {
          throw new InvalidOperationException(string.Format("{0} does not define a StructLayout attribute", type));
        }

        IntPtr sourcePtr = new IntPtr(source);
        return (T)Marshal.PtrToStructure(sourcePtr, typeof(T));
      }
      else
      {
        throw new InvalidOperationException(string.Format("{0} is not supported", type));
      }
    }

    public static string ByteToString(byte[] data)
    {
      return Encoding.ASCII.GetString(data);
    }

    public unsafe static string ByteToString(byte* data, int length)
    {
      byte[] array = Create<byte>(data, length);
      return ByteToString(array);
    }

    public static string HexDump(byte[] bytes)
    {
      if (bytes == null) return "<null>";
      int len = bytes.Length;
      StringBuilder result = new StringBuilder(((len + 15) / 16) * 78);
      char[] chars = new char[78];
      // fill all with blanks
      for (int i = 0; i < 75; i++) chars[i] = ' ';
      chars[76] = '\r';
      chars[77] = '\n';

      for (int i1 = 0; i1 < len; i1 += 16)
      {
        chars[0] = HexChar(i1 >> 28);
        chars[1] = HexChar(i1 >> 24);
        chars[2] = HexChar(i1 >> 20);
        chars[3] = HexChar(i1 >> 16);
        chars[4] = HexChar(i1 >> 12);
        chars[5] = HexChar(i1 >> 8);
        chars[6] = HexChar(i1 >> 4);
        chars[7] = HexChar(i1 >> 0);

        int offset1 = 11;
        int offset2 = 60;

        for (int i2 = 0; i2 < 16; i2++)
        {
          if (i1 + i2 >= len)
          {
            chars[offset1] = ' ';
            chars[offset1 + 1] = ' ';
            chars[offset2] = ' ';
          }
          else
          {
            byte b = bytes[i1 + i2];
            chars[offset1] = HexChar(b >> 8);
            chars[offset1 + 1] = HexChar(b);
            chars[offset2] = (b < 32 ? '·' : (char)b);
          }
          offset1 += (i2 == 8 ? 4 : 3);
          offset2++;
        }
        result.Append(chars);
      }
      return result.ToString();
    }

    private static char HexChar(int value)
    {
      value &= 0xF;
      if (value >= 0 && value <= 9) return (char)('0' + value);
      else return (char)('A' + (value - 10));
    }

    public static void DumpStructure(ICollection<FileBase> root, ILogger logger, int level = 0)
    {
      if (root == null)
        return;

      foreach (var file in root)
      {
        string type = (file.GetType() == typeof(FMS.FAT.Directory)) ? "Directory" : "File     ";
        logger.Log("{0}[{1}] {4}{2} ; {3}", new object[] { "".PadLeft(level * 3, ' '), type, file.name, file.entry.DIR_FileSize.toFileSize(), file.path });
        if (file.GetType() == typeof(FMS.FAT.Directory))
          DumpStructure(((FMS.FAT.Directory)file).children, logger, level + 1);
      }
    }

    public static void Pad(BinaryWriter stream, uint padding_size)
    {
      long toPad = padding_size - stream.BaseStream.Length % padding_size;
      if (toPad > 0)
        stream.Write(new byte[toPad]);
    }

    public static void Pad(ref byte[] data, int padding_size)
    {
      int toPad = padding_size - data.Length % padding_size;
      Array.Resize<byte>(ref data, data.Length + toPad);
    }
  }

  public static class ExtensionMethods
  {
    public static string toFileSize(this uint l)
    {
      if (l == 0)
        return "0 B";

      string[] suf = { "B", "KB", "MB", "GB", "TB", "PB" };
      int place = Convert.ToInt32(Math.Floor(Math.Log(l, 1024)));
      double num = Math.Round(l / Math.Pow(1024, place), 3);
      return num.ToString() + " " + suf[place];
    }
  }
}
