namespace CustomUserManagement
{
    internal static class StringConstants
    {
        public const string ServerDomain = "TcHmiSrv";

        // server domain: names of config variables
        public const string UserGroupUsers = "USERGROUPUSERS";
        public const string UserGroupUsersGroups = "USERGROUPUSERS_GROUPS";

        // server domain: names of function symbols
        public const string GetCurrentUser = "GetCurrentUser";

        // server domain: parameter names
        public const string ClientIp = "clientIp";

        // names of config variables
        public const string CfgUsers = "users";
        public const string CfgUserPassword = "password";
        public const string CfgUserSalt = "salt";
        public const string CfgUserEnabled = "enabled";

        // names of function symbols
        public const string RemoveUserCommand = "RemoveUser";
        public const string AddUserCommand = "AddUser";
        public const string ChangePasswordCommand = "ChangePassword";
        public const string ListUsersCommand = "ListUsers";
        public const string ListDisabledUsersCommand = "ListDisabledUsers";
        public const string EnableUserCommand = "EnableUser";
        public const string DisableUserCommand = "DisableUser";
        public const string RenameUserCommand = "RenameUser";

        // parameter names of function symbols
        public const string Username = "userName";
        public const string Password = "password";
        public const string OldPassword = "oldPassword";
        public const string NewPassword = "newPassword";
        public const string Enabled = "enabled";
        public const string OldUsername = "currentUserName";
        public const string NewUsername = "newUserName";

        // log messages
        public const string MsgAddUserSuccess = "MSG_ADDUSER_SUCCESS";
        public const string MsgInit = "MESSAGE_INIT";
        public const string MsgErrorInit = "ERROR_INIT";

        // extension or server domain: other special string values
        public const string AdminUsername = "__SystemAdministrator";
        public const string DefaultGroup = "__SystemUsers";
    }
}
