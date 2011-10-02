/*
 * This file is part of the FMS project
 *
 * (c) 2011 Sébastien Brochet <blinkseb-nospam-@madalynn.eu>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 */

using System;

namespace FMS
{
  interface ILogger
  {
    void Log(string what);
    void Log(string format, params Object[] args);
  }
}
