/*
 * @Author: RcoIl
 * @Date: 2020/5/26 12:45:23
*/
using Microsoft.Isam.Esent.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpNTDSDumpEx.Resources
{
    /// <summary>
    /// A date time column value based on the LDAP epoch.
    /// </summary>
    internal class LdapDateTimeColumnValue : DateTimeColumnValue
    {
        /// <inheritdoc/>
        protected override void GetValueFromBytes(byte[] value, int startIndex, int count, int err)
        {
            if ((JET_wrn)err == JET_wrn.ColumnNull)
            {
                Value = null;
            }
            else
            {
                CheckDataCount(count);
                var ticks = BitConverter.ToInt64(value, startIndex);
                Value = new DateTime(1601, 1, 1).AddTicks(ticks);
            }
        }
    }
}
