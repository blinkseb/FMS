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

namespace FMS
{
  public class DiskStructure
  {
    public class Directory
    {
      public DirectoryInfo infos;
      public List<Directory> directories;
      public List<File> files;


      public Directory(DirectoryInfo infos)
      {
        this.infos = infos;
      }

      public void Sort()
      {
        directories.Sort(delegate(Directory x, Directory y)
        {
          return x.infos.Name.CompareTo(y.infos.Name);
        });

        files.Sort(delegate(File x, File y)
        {
          if (x.file == null && y.file == null)
            return x.infos.Name.CompareTo(y.infos.Name);

          if (x.file == null)
            return 1; // put after

          if (y.file == null)
            return -1; // put after

          uint xtrack = x.file.Tag.Track;
          uint ytrack = y.file.Tag.Track;

          return (int) (xtrack - ytrack);
        });

        foreach (Directory dir in directories)
          dir.Sort();
      }

      public Directory FindDirectory(string path)
      {
        string[] paths = path.Substring(3).Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
        Directory from = this;
        foreach (string p in paths)
        {
          Directory next = from.directories.Find(delegate(Directory dir)
          {
            return dir.infos.Name.Equals(p, StringComparison.InvariantCultureIgnoreCase);
          });
          if (next == null)
            throw new ArgumentException("path was not found");
          from = next;
        }
        
        return from;
      }

      public int GetIndexOfFile(string name)
      {
        return files.FindIndex(delegate(File x)
        {
          //FIXME: Case sensitive?
          return x.infos.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
        });
      }

      public int GetIndexOfDirectory(string name)
      {
        return directories.FindIndex(delegate(Directory x)
        {
          //FIXME: Case sensitive?
          return x.infos.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
        });
      }
    }

    public class File
    {
      //Directory parent;
      public FileInfo infos;
      public TagLib.File file;

      public File(FileInfo infos)
      {
        this.infos = infos;
      }
    }

    private Directory rootDirectory;
    public Directory Root
    {
      get
      {
        return rootDirectory;
      }
    }

    private string rootFolder;
    public DiskStructure(string root)
    {
      rootFolder = root;
      rootDirectory = new Directory(new DirectoryInfo(rootFolder));
      ReadDirectoryStructure(rootDirectory);
    }

    private void ReadDirectoryStructure(Directory rootDirectory)
    {
      List<File> files = new List<File>();
      foreach (var f in rootDirectory.infos.EnumerateFiles())
      {
        File file = new File(f);
        try
        {
          file.file = TagLib.File.Create(file.infos.FullName);
          if (file.file is TagLib.Image.File)
            file.file = null;
        }
        catch (TagLib.UnsupportedFormatException)
        {
          file.file = null;
        }

        files.Add(file);
      }

      List<Directory> directories = new List<Directory>();
      foreach (var f in rootDirectory.infos.EnumerateDirectories())
        directories.Add(new Directory(f));

      rootDirectory.files = files;
      rootDirectory.directories = directories;

      foreach (var directory in directories)
      {
        ReadDirectoryStructure(directory);
      }
    }

    public void Sort()
    {
      rootDirectory.Sort();
    }
  }
}
