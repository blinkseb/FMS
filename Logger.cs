/*
 * This file is part of the FMS project
 *
 * (c) 2011 Sébastien Brochet <blinkseb-nospam-@madalynn.eu>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 */

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
