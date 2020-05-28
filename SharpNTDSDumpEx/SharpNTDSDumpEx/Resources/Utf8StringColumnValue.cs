/*
 * @Author: RcoIl
 * @Date: 2020/5/21 17:54:30
*/
using Microsoft.Isam.Esent.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpNTDSDumpEx.Resources
{
    class Utf8StringColumnValue : StringColumnValue
    {
        protected override void GetValueFromBytes(byte[] value, int startIndex, int count, int err)
        {
            Value = Encoding.UTF8.GetString(value, startIndex, count);
        }
    }
}
