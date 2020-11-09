//-----------------------------------------------------------------------
// <copyright file="ExtensionSpecificError.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CustomUserManagement
{
    public enum ExtensionSpecificError
    {
        SUCCESS = 0,
        CANNOT_SET_GROUP_ACCESS,
        USER_NOT_FOUND,
        USER_ALREADY_EXISTS,
        INVALID_PARAMETER,
        FAILED,
        INTERNAL_ERROR
    }
}
