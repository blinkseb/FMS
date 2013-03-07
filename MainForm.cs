using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using FMS.FAT;

namespace FMS
{
  public partial class MainForm : Form
  {
    Logger logger;

    public MainForm()
    {
      InitializeComponent();
      logger = new Logger(txtLog);
    }

    private void button1_Click_1(object sender, EventArgs e)
    {
      DialogResult result = folderBrowser.ShowDialog();
      if (result == DialogResult.OK)
        txtFolder.Text = folderBrowser.SelectedPath;
    }

    private void txtFolder_TextChanged(object sender, EventArgs e)
    {
      btnDump.Enabled = btnDumpAndSort.Enabled = writeFat.Enabled = System.IO.Directory.Exists(((TextBox)sender).Text);
    }

    private void btnStart_Click(object sender, EventArgs e)
    {
    }

    private void btnDump_Click(object sender, EventArgs e)
    {
      FAT.FATReader fatReader = null;
      try
      {
        fatReader = new FAT.FATReader(txtFolder.Text, logger);
        fatReader.Open(FileAccess.Read);
        fatReader.DumpFAT();
      }
      finally
      {
        if (fatReader != null)
          fatReader.Close();
      }
    }

    private void writeFat_Click(object sender, EventArgs e)
    {
      FAT.FATReader fatReader = null;
      DiskStructure tagDiskStructure = null;
      try
      {
        tagDiskStructure = new DiskStructure(txtFolder.Text);
        tagDiskStructure.Sort();

        fatReader = new FAT.FATReader(txtFolder.Text, logger);
        fatReader.Open(FileAccess.ReadWrite);

        fatReader.Sort(tagDiskStructure);
        fatReader.DumpFAT();

        fatReader.WriteFAT();
      }
      finally
      {
        if (fatReader != null)
          fatReader.Close();
      }
    }

    private void btnDumpAndSort_Click(object sender, EventArgs e)
    {
      FAT.FATReader fatReader = null;
      DiskStructure tagDiskStructure = null;
      try
      {
        tagDiskStructure = new DiskStructure(txtFolder.Text);
        tagDiskStructure.Sort();

        fatReader = new FAT.FATReader(txtFolder.Text, logger);
        fatReader.Open(FileAccess.Read);
        fatReader.Sort(tagDiskStructure);
        fatReader.DumpFAT();
      }
      finally
      {
        if (fatReader != null)
          fatReader.Close();
      }
    }
  }
}
