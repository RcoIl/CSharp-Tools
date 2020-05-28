/*
 * @Author: RcoIl
 * @Date: 2020/5/26 16:56:56
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace SharpNTDSDumpEx.Resources
{
    /// <summary>
    /// Provides information extracted from NTDS related to a domain.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    internal class DomainInfo
    {
        /// <summary>
        /// Gets or sets the SID of the Administrators group.
        /// </summary>
        internal SecurityIdentifier AdministratorsSid { get; set; }

        /// <summary>
        /// Gets or sets the Distinguised Name of the domain.
        /// </summary>
        internal string Dn { get; set; }

        /// <summary>
        /// Gets or sets the SID of the Domain Admin group.
        /// </summary>
        internal SecurityIdentifier DomainAdminsSid { get; set; }

        /// <summary>
        /// Gets or sets the SID of the Enterprise Admins group.
        /// </summary>
        internal SecurityIdentifier EnterpriseAdminsSid { get; set; }

        /// <summary>
        /// Gets or sets the FQDN of the domain.
        /// </summary>
        internal string Fqdn { get; set; }

        /// <summary>
        /// Gets or sets the name of the domain.
        /// </summary>
        internal string Name { get; set; }

        /// <summary>
        /// Gets or sets the SID of the domain.
        /// </summary>
        internal SecurityIdentifier Sid { get; set; }
    }
}
