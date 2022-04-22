//-----------------------------------------------------------------------
// <copyright file="LetsEncrypt.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using Certes;
using Certes.Acme;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;
using TcHmiSrv.Core.Tools.Settings;

namespace LetsEncrypt
{
    public class LetsEncrypt : IServerExtension
    {
        // path of challenge file
        private static readonly string CHAL_FILE_PATH = "www/.well-known/acme-challenge/";

        // config properties
        private static readonly string CFG_GENERATE_CERTIFICATE = "generateCertificate";
        private static readonly string CFG_DATA = "data";
        private static readonly string CFG_CONTACTS = "contacts";
        private static readonly string CFG_DOMAIN = "domain";
        private static readonly string CFG_API = "api";
        private static readonly string CFG_CERTIFICATE_INFORMATION = "certificateInformation";
        private static readonly string CFG_COUNTRY_NAME = "countryName";
        private static readonly string CFG_STATE = "state";
        private static readonly string CFG_LOCALITY = "locality";
        private static readonly string CFG_ORGANIZATION = "organization";
        private static readonly string CFG_INTERVAL = "interval";
        private static readonly string CFG_INTERVAL_STAGING = "intervalStaging";

        private const int LetsEncryptV2 = 0;
        private const int LetsEncryptStagingV2 = 1;

        // config hints
        private static readonly string HINT_WILDCARD_SUPPORT = "WILDCARD_DOMAIN_NOT_SUPPORTED";
        private static readonly string HINT_WRONG_PORT_CONFIGURATION = "WRONG_PORT_CONFIGURATION";
        private static readonly string HINT_NO_CHALLENGE_FILE_ACCESS = "NO_CHALLENGE_FILE_ACCESS";
        private static readonly string HINT_RESTART_REQUIRED = "RESTART_REQUIRED";

        private static readonly int HINT_ID_WILDCARD_SUPPORT = 100;
        private static readonly int HINT_ID_WRONG_PORT_CONFIGURATION = 101;
        private static readonly int HINT_ID_NO_CHALLENGE_FILE_ACCESS = 102;
        private static readonly int HINT_ID_RESTART_REQUIRED = 103;

        private object _shutdownLock = new object();

        private RequestListener _requestListener = new RequestListener();
        private ShutdownListener _shutdownListener = new ShutdownListener();
        private ConfigListener _configListener = new ConfigListener();

        private X509Certificate2 _currentCertificate = null;
        private IKey _currentKey = null;

        private bool _portCheck = false;
        private bool _initFinisched = false;

        private AcmeContext _acmeContext;

        private System.Timers.Timer _certGenerationTimer = null;
        private DateTime? _nextCertGeneration;

        private string _intervalPath;
        private string _accountKeyFile;

        private void ReadCurrentCertificate()
        {
            // get the current certificate via http request

            string serverDomain = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_DOMAIN);
            string serverUrl = string.Format("http://{0}", serverDomain);

            if (string.IsNullOrEmpty(serverDomain))
            {
                TcHmiAsyncLogger.Send(Severity.Warning, "ERROR_GET_CERTIFICATE_EMPTY_DOMAIN");
                return;
            }

            HttpWebRequest request = WebRequest.CreateHttp(serverUrl);

            string serverId = null;
            HttpWebResponse response = null;
            string error = "";

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex) 
            {
                error = ex.Message;
                response = (HttpWebResponse)ex.Response;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            if (response == null)
            {
                TcHmiAsyncLogger.Send(Severity.Warning, "ERROR_GET_CERTIFICATE", new string[] { serverUrl, error });
                return;
            }

            string setCookieArg = response.Headers["Set-Cookie"];

            if (!string.IsNullOrEmpty(setCookieArg))
            {
                string[] cookies = setCookieArg.Replace(" ", "").Split(";");
                // structure of the server's session cookie: sessionId-{serverId}={sessionId}
                foreach (string cookie in cookies)
                {
                    if (cookie.StartsWith("sessionId-"))
                    {
                        serverId = cookie.Replace("sessionId-", "");
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(serverId))
            {
                TcHmiAsyncLogger.Send(Severity.Info, "ERROR_GET_CERTIFICATE_SERVERID");
                return;
            }

            request = WebRequest.CreateHttp(serverUrl + "/ExportCertificate");
            request.AutomaticDecompression = DecompressionMethods.GZip;

            var container = new CookieContainer();
            container.Add(new Uri(serverUrl), new Cookie("sessionId", TcHmiApplication.Context.Session.Id));

            request.CookieContainer = container;
            string content = "";

            try
            {
                response = request.GetResponse() as HttpWebResponse;

                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    content = reader.ReadToEnd();

                _currentCertificate = new X509Certificate2(Encoding.ASCII.GetBytes(content));
            }
            catch (Exception ex)
            {
                TcHmiAsyncLogger.Send(Severity.Error, "ERROR_READ_CERTIFICATE", new string[] { serverUrl + "/ExportCertificate", ex.Message });
            }
        }

        public ErrorValue Init()
        {
            Context c = TcHmiApplication.Context;
            c.Domain = "TcHmiSrv";

            try
            {
                // add event handlers
                _requestListener.OnRequest += OnRequest;
                _shutdownListener.OnShutdown += OnShutdown;
                _configListener.OnChange += OnChange;
                _configListener.BeforeChange += BeforeChange;

                // register listeners
                TcHmiApplication.AsyncHost.RegisterListener(TcHmiApplication.Context, _shutdownListener);
                TcHmiApplication.AsyncHost.RegisterListener(TcHmiApplication.Context, _configListener, ConfigListenerSettings.Default);

                int api = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_API);
                bool generateCert = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CFG_GENERATE_CERTIFICATE);

                SetCurrentContext(api);

                // start timer for certificate generation
                _certGenerationTimer = new System.Timers.Timer(1000);
                _certGenerationTimer.Enabled = generateCert;
                _certGenerationTimer.Elapsed += GenerateCertificateCallback;
                _certGenerationTimer.Start();

                // reset "restart required"-hint
                ConfigurationHint(HINT_RESTART_REQUIRED, HINT_ID_RESTART_REQUIRED, true);

                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                TcHmiAsyncLogger.Send(Severity.Error, "ERROR_INIT", new string[] { ex.Message });
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        private void SetCurrentContext(int api)
        {
            Uri uri;

            // set Let's Encrypt api
            switch (api)
            {
                case LetsEncryptV2:
                    _accountKeyFile = "account-key-v2.pem";
                    uri = WellKnownServers.LetsEncryptV2;
                    _intervalPath = CFG_INTERVAL;
                    break;
                case LetsEncryptStagingV2:
                    _accountKeyFile = "account-key-staging-v2.pem";
                    uri = WellKnownServers.LetsEncryptStagingV2;
                    _intervalPath = CFG_INTERVAL_STAGING;
                    break;
                default: return;
            }

            if (File.Exists(_accountKeyFile))
            {
                // generate new acme context
                var accountKey = KeyFactory.FromPem(File.ReadAllText(_accountKeyFile));
                _acmeContext = new AcmeContext(uri, accountKey);
            }
            else
            {
                // read existing acme context
                _acmeContext = new AcmeContext(uri);

                List<string> contacts = new List<string>();
                Value mails = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_CONTACTS);
                foreach (Value mail in mails)
                    contacts.Add("mailto:" + System.Web.HttpUtility.UrlEncode(mail.GetString()));

                _acmeContext.NewAccount(contacts, true);
                var newKey = _acmeContext.AccountKey.ToPem();
                File.WriteAllText(_accountKeyFile, newKey);
            }
        }

        private double GetCurrentRestInterval()
        {
            TimeSpan currentDuration = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, _intervalPath); ;
            DateTime expirationDate = DateTime.Now;

            try
            {
                expirationDate = _currentCertificate.NotBefore.AddMilliseconds(currentDuration.TotalMilliseconds);
            }
            catch (Exception /*unused*/) { _currentCertificate = null; }

            if (_currentCertificate == null)
            {
                _nextCertGeneration = DateTime.Now.AddMilliseconds(5000);
                return 5000;
            }

            _nextCertGeneration = expirationDate;

            return expirationDate.Subtract(DateTime.Now).TotalMilliseconds;
        }

        private void GenerateCertificateCallback(object source, ElapsedEventArgs state)
        {
            _certGenerationTimer.Stop();

            CheckServerConfiguration();

            if (_portCheck && _currentCertificate == null)
                ReadCurrentCertificate();

            double restTime = GetCurrentRestInterval();
            DateTime now = DateTime.Now;
            ErrorValue err = ErrorValue.HMI_SUCCESS;

            if (_portCheck && (_currentCertificate == null || restTime <= 0))
            {
                try
                {
                    GenerateCertificate();
                }
                catch (Exception e)
                {
                    TcHmiAsyncLogger.Send(Severity.Error, "ERROR_CREATE_CERTIFICATE", new string[] { e.Message });
                    StopCertificateGeneration();
                    return;
                }
            }

            restTime = (err == ErrorValue.HMI_SUCCESS ? GetCurrentRestInterval() : 5000);

            if (_certGenerationTimer == null)
            {
                _certGenerationTimer = new System.Timers.Timer(restTime);
                _certGenerationTimer.Elapsed += GenerateCertificateCallback;
            }
            else if (restTime > 0)
            {
                _certGenerationTimer.Interval = restTime;
            }

            _certGenerationTimer.Start();
        }

        private void ConfigurationHint(string msg, int id, bool reset = false)
        {
            // send configuration hint

            Context serverContext = TcHmiApplication.Context;
            serverContext.Domain = "TcHmiSrv";

            Event evt = new Event(serverContext, "CONFIGURATION_HINT");
            evt.TimeReceived = DateTime.Now;
            Alarm payload = new Alarm(TcHmiApplication.Context, msg);
            payload.Severity = Severity.Warning;
            payload.Id = id;
            payload.ConfirmationState = reset ? AlarmConfirmationState.Reset : AlarmConfirmationState.Wait;
            evt.Payload = payload;

            TcHmiApplication.AsyncHost.SendAsync(serverContext, evt);
        }

        /// <summary>
        /// Generate new certificate with Let's Encrypt api.
        /// </summary>
        /// <returns>ErrprValue</returns>
        private void GenerateCertificate()
        {
            TcHmiAsyncLogger.Send(Severity.Info, "INFO_GENERATE_CERTIFICATE");

            string domain = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_DOMAIN);

            if (_currentCertificate != null && _currentKey != null)
                _acmeContext.RevokeCertificate(_currentCertificate.RawData, Certes.Acme.Resource.RevocationReason.KeyCompromise, _currentKey);
            else if (_currentCertificate != null)
                TcHmiAsyncLogger.Send(Severity.Info, "ERROR_REVOKE_CERTIFICATE");

            IOrderContext order = _acmeContext.NewOrder(new[] { domain }).GetAwaiter().GetResult();

            IChallengeContext challenge;
            IAuthorizationContext authorization = order.Authorizations().GetAwaiter().GetResult().First();

            // handle Let's Encrypt challenges
            if (domain.Contains("*"))
            {
                // DNS challenge is not supported yet
                throw new TcHmiException("Domain should not be wildcard", ErrorValue.HMI_E_FAIL);
            }
            else
            {
                // handle acme http challenge

                if (Directory.Exists(CHAL_FILE_PATH))
                    Directory.Delete(CHAL_FILE_PATH, true);

                challenge = authorization.Http().GetAwaiter().GetResult();
                Directory.CreateDirectory(CHAL_FILE_PATH);
                File.WriteAllText(CHAL_FILE_PATH + challenge.Token, challenge.KeyAuthz);

                var challengeValidator = challenge.Validate().GetAwaiter().GetResult();

                int maxAttempts = 5;
                do
                {
                    challengeValidator = challenge.Resource().GetAwaiter().GetResult();
                } while (challengeValidator.Status == Certes.Acme.Resource.ChallengeStatus.Pending && maxAttempts-- >= 0);

                if (challengeValidator.Status == Certes.Acme.Resource.ChallengeStatus.Invalid)
                {
                    if (challengeValidator.Error.Status == HttpStatusCode.Unauthorized)
                        ConfigurationHint(HINT_NO_CHALLENGE_FILE_ACCESS, HINT_ID_NO_CHALLENGE_FILE_ACCESS);
                    TcHmiAsyncLogger.Send(Severity.Error, "INFO_CHALLENGE_ERROR", new string[] { challengeValidator.Error.Detail });
                }
                else if (challengeValidator.Status != Certes.Acme.Resource.ChallengeStatus.Valid)
                    TcHmiAsyncLogger.Send(Severity.Error, "INFO_CHALLENGE_STATUS", new string[] { challengeValidator.Status.ToString() });

                ConfigurationHint(HINT_NO_CHALLENGE_FILE_ACCESS, HINT_ID_NO_CHALLENGE_FILE_ACCESS, true);
            }

            // generate certificate from order
            var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
            Value infos = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_CERTIFICATE_INFORMATION);
            CsrInfo csrInfo = new CsrInfo
            {
                CountryName = infos[CFG_COUNTRY_NAME].ToString(),
                State = infos[CFG_STATE].ToString(),
                Locality = infos[CFG_LOCALITY].ToString(),
                Organization = infos[CFG_ORGANIZATION].ToString()
            };

            CertificateChain certChain = order.Generate(csrInfo, certKey).GetAwaiter().GetResult();

            // set generated certificate in server config
            Context c = TcHmiApplication.Context;
            c.Domain = "TcHmiSrv";

            CommandGroup cmdGroup = new CommandGroup("TcHmiSrv", new List<Command>()
            {
                new Command("TcHmiSrv.Config::CERTIFICATE")
                {
                    WriteValue = new Value(Encoding.ASCII.GetBytes(certChain.Certificate.ToPem()))
                },
                new Command("TcHmiSrv.Config::KEY")
                {
                    WriteValue = new Value(Encoding.ASCII.GetBytes(certKey.ToPem()))
                }
            });

            ErrorValue err = TcHmiApplication.AsyncHost.Execute(ref c, ref cmdGroup);
            if (err != ErrorValue.HMI_SUCCESS)
                throw new TcHmiException("Setting certificate and key in config failed", err);
            else
            {
                // show "restart required"-hint
                TcHmiAsyncLogger.Send(Severity.Info, "INFO_CERTIFICATE_SUCCEEDED");
                ConfigurationHint(HINT_RESTART_REQUIRED, HINT_ID_RESTART_REQUIRED);
            }

            _currentCertificate = new X509Certificate2(Encoding.ASCII.GetBytes(certChain.Certificate.ToPem()));
            _currentKey = certKey;
        }

        private void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            foreach (var command in e.Commands)
            {
                if (command.Name.EndsWith("Diagnostics"))
                {
                    Value information = new Value();

                    if ((_currentCertificate != null))
                    {
                        Value currentCertificate = new Value();
                        bool valid = false;

                        try
                        {
                            currentCertificate.Add("validTo", _currentCertificate.NotAfter);
                            currentCertificate.Add("validFrom", _currentCertificate.NotBefore);
                            valid = (_currentCertificate != null && (_currentCertificate.NotAfter.Subtract(DateTime.Now).TotalMilliseconds > 0));
                        }
                        catch (Exception /*unused*/) { _currentCertificate = null; }

                        currentCertificate.Add("valid", valid);
                        information.Add("currentCertificate", currentCertificate);
                    }

                    information.Add("nextCertificateGeneration", _nextCertGeneration);

                    command.ReadValue = information;
                }
            }
        }

        private void BeforeChange(object sender, TcHmiSrv.Core.Listeners.ConfigListenerEventArgs.BeforeChangeEventArgs e)
        {
            if (e.Path == CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_DOMAIN)
            {
                string domain = e.Value;
                if (!Uri.IsWellFormedUriString(domain, UriKind.RelativeOrAbsolute))
                {
                    throw new TcHmiException("Please enter a valid uri string", ErrorValue.HMI_E_INVALID_PARAMETER);
                }

                bool resetHint = !(domain.Contains("*"));
                ConfigurationHint(HINT_WILDCARD_SUPPORT, HINT_ID_WILDCARD_SUPPORT, resetHint);
            }
            else if (string.Compare(e.Path, CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_CONTACTS + "[") == 0)
            {
                string mail = e.Value;
                if (!Regex.IsMatch(mail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase))
                {
                    throw new TcHmiException("Please enter a valid mail address", ErrorValue.HMI_E_INVALID_PARAMETER);
                }
            }
        }

        private void OnChange(object sender, TcHmiSrv.Core.Listeners.ConfigListenerEventArgs.OnChangeEventArgs e)
        {
            bool generateCert = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CFG_GENERATE_CERTIFICATE);

            if (e.Path == CFG_DATA || e.Path == CFG_INTERVAL)
            {
                _certGenerationTimer.Stop();
                int api = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_API);
                SetCurrentContext(api);

                if (e.Path == CFG_DATA)
                {
                    if (_initFinisched && generateCert)
                    {
                        try
                        {
                            GenerateCertificate();
                        }
                        catch (Exception ex)
                        {
                            TcHmiAsyncLogger.Send(Severity.Error, "ERROR_CREATE_CERTIFICATE", new string[] { ex.Message });
                            StopCertificateGeneration();
                            generateCert = false;
                        }
                    }
                    else
                        _initFinisched = true;
                }

                double currentInterval = GetCurrentRestInterval();
                if (currentInterval <= 0) _certGenerationTimer.Interval = 5000;
                else _certGenerationTimer.Interval = currentInterval;

                if (generateCert) _certGenerationTimer.Start();
            }
            else if (e.Path == CFG_GENERATE_CERTIFICATE)
            {
                if (generateCert)
                {
                    ReadCurrentCertificate();
                    double interval = GetCurrentRestInterval();

                    if (interval < 0) interval = 5000;
                    _certGenerationTimer.Interval = interval;
                    _certGenerationTimer.Start();
                }
                else
                {
                    _certGenerationTimer.Stop();
                    _nextCertGeneration = null;
                }
            }
            else if (e.Path.StartsWith(CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_CONTACTS))
            {
                IList<string> contacts = new List<string>();
                Value mails = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CFG_DATA + TcHmiApplication.PathElementSeparator + CFG_CONTACTS);
                foreach (Value mail in mails)
                {
                    contacts.Add("mailto:" + mail.GetString());
                }

                try
                {
                    _acmeContext.Account().GetAwaiter().GetResult().Update(contacts);
                }
                catch (Exception)
                {
                    try
                    {
                        _acmeContext.NewAccount(contacts, true).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        TcHmiAsyncLogger.Send(Severity.Error, "ERROR_GENERATE_ACCOUNT", new string[] { ex.Message });
                    }
                }

                var newKey = _acmeContext.AccountKey.ToPem();
                File.WriteAllText(_accountKeyFile, newKey);
            }
            else if (e.Path == CFG_INTERVAL_STAGING || e.Path == CFG_INTERVAL)
            {
                _certGenerationTimer.Stop();

                double interval = GetCurrentRestInterval();

                if (interval < 0) interval = 5000;
                _certGenerationTimer.Interval = interval;
                _certGenerationTimer.Start();
            }
        }

        private void OnShutdown(object sender, TcHmiSrv.Core.Listeners.ShutdownListenerEventArgs.OnShutdownEventArgs e)
        {
            // If the extension does not shutdown after 10 seconds (blocking threads) OnShutdown will be called again
            lock (_shutdownLock)
            {
                Context context = e.Context;

                try
                {
                    // Unregister listeners
                    TcHmiApplication.AsyncHost.UnregisterListener(context, _shutdownListener);
                    TcHmiApplication.AsyncHost.UnregisterListener(context, _configListener);
                }
                catch (Exception)
                {
                }
            }
        }

        private void CheckServerConfiguration()
        {
            var serverContext = TcHmiApplication.Context;
            serverContext.Domain = "TcHmiSrv";

            // ports 80 and 443 must be configured for Let's Encrypt

            var group = new CommandGroup("TcHmiSrv", new List<Command>() { 
                new Command("TcHmiSrv.Config::ENDPOINTS")
            });

            ErrorValue err = TcHmiApplication.AsyncHost.Execute(ref serverContext, ref group);

            _portCheck = false;
            if (err != ErrorValue.HMI_SUCCESS)
                return;
            
            Command endpointsCmd = group[0];
            if (!endpointsCmd.ReadValue.IsVector)
                return;

            bool port443Found = false;
            bool port80Found = false;

            foreach (Value item in endpointsCmd.ReadValue)
            {
                string endpoint = item.GetString();
                port443Found |= (endpoint == "https://0.0.0.0:443");
                port80Found |= (endpoint == "http://0.0.0.0:80");
            }

            _portCheck = port443Found && port80Found;

            if (!_portCheck)
                StopCertificateGeneration();

            ConfigurationHint(HINT_WRONG_PORT_CONFIGURATION, HINT_ID_WRONG_PORT_CONFIGURATION, _portCheck);
        }

        private void StopCertificateGeneration()
        {
            _nextCertGeneration = null;
            _certGenerationTimer.Stop();
            TcHmiApplication.AsyncHost.SetConfigValue(TcHmiApplication.Context, CFG_GENERATE_CERTIFICATE, new Value(false));
        }
    }
}
