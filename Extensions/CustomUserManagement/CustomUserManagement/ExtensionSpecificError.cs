//-----------------------------------------------------------------------
// <copyright file="ExtensionSpecificError.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CustomUserManagement
{
    public enum ExtensionSpecificError
    {
        Success = 0,
        CannotSetGroupAccess,
        UserNotFound,
        UserAlreadyExists,
        InvalidParameter,
        Failed,
        InternalError
    }
}
