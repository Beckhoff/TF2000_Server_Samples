namespace CustomUserManagement
{
    internal static class StringConstants
    {
        public const string SERVER_DOMAIN = "TcHmiSrv";

        // server domain: names of config variables
        public const string USERGROUPUSERS = "USERGROUPUSERS";
        public const string USERGROUPUSERS_GROUPS = "USERGROUPUSERS_GROUPS";
        public const string AUTO_LOGOFF = "AUTO_LOGOFF";

        // server domain: names of function symbols
        public const string GET_CURRENT_USER = "GetCurrentUser";

        // server domain: parameter names
        public const string CLIENT_IP = "clientIp";

        // names of config variables
        public const string CFG_USERS = "users";
        public const string CFG_USER_PASSWORD = "password";
        public const string CFG_USER_SALT = "salt";
        public const string CFG_USER_ENABLED = "enabled";

        // names of function symbols
        public const string REMOVE_USER_COMMAND = "RemoveUser";
        public const string ADD_USER_COMMAND = "AddUser";
        public const string CHANGE_PASSWORD_COMMAND = "ChangePassword";
        public const string LIST_USERS_COMMAND = "ListUsers";
        public const string LIST_DISABLED_USERS_COMMAND = "ListDisabledUsers";
        public const string ENABLE_USER_COMMAND = "EnableUser";
        public const string DISABLE_USER_COMMAND = "DisableUser";
        public const string RENAME_USER_COMMAND = "RenameUser";

        // parameter names of function symbols
        public const string USERNAME = "userName";
        public const string PASSWORD = "password";
        public const string OLD_PASSWORD = "oldPassword";
        public const string NEW_PASSWORD = "newPassword";
        public const string ENABLED = "enabled";
        public const string OLD_USERNAME = "currentUserName";
        public const string NEW_USERNAME = "newUserName";

        // log messages
        public const string MSG_ADDUSER_SUCCESS = "MSG_ADDUSER_SUCCESS";
        public const string MSG_INIT = "MESSAGE_INIT";
        public const string MSG_ERROR_INIT = "ERROR_INIT";

        // extension or server domain: other special string values
        public const string ADMIN_USERNAME = "__SystemAdministrator";
        public const string GUEST_USERNAME = "__SystemGuest";
        public const string ADMIN_GROUP = "__SystemAdministrators";
        public const string GUEST_GROUP = "__SystemGuests";
        public const string DEFAULT_GROUP = "__SystemUsers";
        public const string DEFAULT_LOCALE = "client";
    }
}
