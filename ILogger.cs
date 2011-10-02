using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FMS
{
  interface ILogger
  {
    void Log(string what);
    void Log(string format, params Object[] args);
  }
}
