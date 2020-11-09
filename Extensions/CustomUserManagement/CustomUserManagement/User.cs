using System;
using TcHmiSrv.Core;

namespace CustomUserManagement
{
    internal class User
    {
        private Value _configValue = new Value();
        public Value ConfigValue
        {
            get { return _configValue; }
        }

        public User(Value configValue)
        {
            _configValue = configValue;
        }

        public ErrorValue CheckCredentials(string username, string password)
        {
            if (ConfigValue.TryGetValue(StringConstants.CFG_USER_ENABLED, out var enabled) && (!enabled))
            {
                return ErrorValue.HMI_E_AUTH_DISABLED;
            }

            string correctHash = ConfigValue[StringConstants.CFG_USER_PASSWORD];
            string hash = ComputePasswordHash(password);
            return hash == correctHash ? ErrorValue.HMI_SUCCESS : ErrorValue.HMI_E_AUTH_FAILED;
        }

        public static ExtensionSpecificError CreateUser(out User user, string name, string password, bool enabled)
        {
            // factory function

            if (password.Length == 0 || name.Length == 0)
            {
                user = null;
                return ExtensionSpecificError.INVALID_PARAMETER;
            }

            (string hash, byte[] salt) result = StaticComputePasswordHash(password);
            user = new User(name, result.hash, result.salt, enabled);

            return ExtensionSpecificError.SUCCESS;
        }

        public static string UsernameFromSession(string sessionUser)
        {
            // extdomain::username or username ---> username
            var parts = sessionUser.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return parts[1];
            }
            return sessionUser;
        }

        private string ComputePasswordHash(string password)
        {
            byte[] saltBytes = ConfigValue.TryGetValue(StringConstants.CFG_USER_SALT, out var salt) ? (byte[])salt : HashHelper.GenerateSalt();
            return HashHelper.Sha256(saltBytes, password);
        }

        private static (string hash, byte[] salt) StaticComputePasswordHash(string password)
        {
            byte[] salt = HashHelper.GenerateSalt();
            return (HashHelper.Sha256(salt, password), salt);
        }

        private User(string name, string password_hash, byte[] salt, bool enabled)
        {
            ConfigValue.Add(StringConstants.CFG_USER_PASSWORD, password_hash);
            ConfigValue.Add(StringConstants.CFG_USER_SALT, salt);
            ConfigValue.Add(StringConstants.CFG_USER_ENABLED, enabled);
        }
    }
}
