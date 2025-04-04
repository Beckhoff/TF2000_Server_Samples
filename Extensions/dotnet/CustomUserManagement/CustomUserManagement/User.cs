using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;

namespace CustomUserManagement
{
    internal class User
    {
        public User(Value configValue)
        {
            ConfigValue = configValue;
        }

        private User(string passwordHash, byte[] salt, bool enabled)
        {
            ConfigValue.Add(StringConstants.CfgUserPassword, passwordHash);
            ConfigValue.Add(StringConstants.CfgUserSalt, salt);
            ConfigValue.Add(StringConstants.CfgUserEnabled, enabled);
        }

        public Value ConfigValue { get; } = new Value();

        public ErrorValue CheckCredentials(string password)
        {
            if (ConfigValue.TryGetValue(StringConstants.CfgUserEnabled, out var enabled) && !enabled)
            {
                return ErrorValue.HMI_E_AUTH_DISABLED;
            }

            string correctHash = ConfigValue[StringConstants.CfgUserPassword];
            var hash = ComputePasswordHash(password);
            return hash == correctHash ? ErrorValue.HMI_SUCCESS : ErrorValue.HMI_E_AUTH_FAILED;
        }

        public static ExtensionSpecificError CreateUser(out User user, string name, string password, bool enabled)
        {
            // factory function

            if (password.Length == 0 || name.Length == 0)
            {
                user = null;
                return ExtensionSpecificError.InvalidParameter;
            }

            var (hash, salt) = StaticComputePasswordHash(password);
            user = new User(hash, salt, enabled);

            return ExtensionSpecificError.Success;
        }

        public static string UsernameFromSession(string sessionUser)
        {
            // extdomain::username or username ---> username
            var parts = TcHmiApplication.SplitPath(sessionUser, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1)
            {
                return parts[1];
            }

            return sessionUser;
        }

        private string ComputePasswordHash(string password)
        {
            var saltBytes = ConfigValue.TryGetValue(StringConstants.CfgUserSalt, out var salt)
                ? (byte[])salt
                : HashHelper.GenerateSalt();
            return HashHelper.Sha256(saltBytes, password);
        }

        private static (string hash, byte[] salt) StaticComputePasswordHash(string password)
        {
            var salt = HashHelper.GenerateSalt();
            return (HashHelper.Sha256(salt, password), salt);
        }
    }
}
