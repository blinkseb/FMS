using System;

namespace FMS
{
  interface ILogger
  {
    void Log(string what);
    void Log(string format, params Object[] args);
  }
}
