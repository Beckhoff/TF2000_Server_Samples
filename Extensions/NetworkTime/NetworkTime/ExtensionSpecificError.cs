//-----------------------------------------------------------------------
// <copyright file="ExtensionSpecificError.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace NetworkTime
{
    public enum ExtensionSpecificError
    {
        SUCCESS = 0,
        INTERNAL_ERROR,
        NTP_SERVER_NOT_AVAILABLE
    }
}
