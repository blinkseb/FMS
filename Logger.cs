using System;
using System.Windows.Forms;

namespace FMS
{
  class Logger : ILogger
  {
    private TextBox where;
    public Logger(TextBox txtBox)
    {
      where = txtBox;
    }

    public void Log(string what)
    {
      where.AppendText(what + "\n");
    }

    public void Log(string format, params Object[] args)
    {
      where.AppendText(String.Format(format, args) + "\n");
    }
  }
}
