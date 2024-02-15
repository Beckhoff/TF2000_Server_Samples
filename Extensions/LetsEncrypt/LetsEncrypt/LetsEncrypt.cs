//-----------------------------------------------------------------------
// <copyright file="LetsEncrypt.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Web;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.ConfigListenerEventArgs;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;
using TcHmiSrv.Core.Tools.Settings;
using Directory = System.IO.Directory;

namespace LetsEncrypt
{
    // ReSharper disable once UnusedType.Global
    public class LetsEncrypt : IServerExtension
    {
        private const int LetsEncryptV2 = 0;

        private const int LetsEncryptStagingV2 = 1;

        // path of challenge file
        private const string ChallengeFilePath = "www/.well-known/acme-challenge/";

        // config properties
        private const string CfgGenerateCertificate = "generateCertificate";
        private const string CfgData = "data";
        private const string CfgContacts = "contacts";
        private const string CfgDomain = "domain";
        private const string CfgApi = "api";
        private const string CfgCertificateInformation = "certificateInformation";
        private const string CfgCountryName = "countryName";
        private const string CfgState = "state";
        private const string CfgLocality = "locality";
        private const string CfgOrganization = "organization";
        private const string CfgInterval = "interval";
        private const string CfgIntervalStaging = "intervalStaging";

        // config hints
        private const string HintWildcardSupport = "WILDCARD_DOMAIN_NOT_SUPPORTED";
        private const string HintWrongPortConfiguration = "WRONG_PORT_CONFIGURATION";
        private const string HintNoChallengeFileAccess = "NO_CHALLENGE_FILE_ACCESS";
        private const string HintRestartRequired = "RESTART_REQUIRED";

        private const int HintIdWildcardSupport = 100;
        private const int HintIdWrongPortConfiguration = 101;
        private const int HintIdNoChallengeFileAccess = 102;
        private const int HintIdRestartRequired = 103;
        private readonly ConfigListener _configListener = new ConfigListener();

        private readonly RequestListener _requestListener = new RequestListener();
        private string _accountKeyFile;

        private AcmeContext _acmeContext;

        private Timer _certGenerationTimer;

        private X509Certificate2 _currentCertificate;
        private IKey _currentKey;

        private bool _initFinished;

        private string _intervalPath;
        private DateTime? _nextCertGeneration;

        public ErrorValue Init()
        {
            var c = TcHmiApplication.Context;
            c.Domain = "TcHmiSrv";

            try
            {
                // add event handlers
                _requestListener.OnRequest += OnRequest;
                _configListener.OnChange += OnChange;
                _configListener.BeforeChange += BeforeChange;

                // register listeners
                TcHmiApplication.AsyncHost.RegisterListener(TcHmiApplication.Context, _configListener,
                    ConfigListenerSettings.Default);

                int api = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context,
                    CfgData + TcHmiApplication.PathElementSeparator + CfgApi);
                bool generateCert =
                    TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CfgGenerateCertificate);

                SetCurrentContext(api);

                // start timer for certificate generation
                _certGenerationTimer = new Timer(1000) { Enabled = generateCert };
                _certGenerationTimer.Elapsed += GenerateCertificateCallback;
                _certGenerationTimer.Start();

                // reset "restart required"-hint
                ConfigurationHint(HintRestartRequired, HintIdRestartRequired, true);

                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                _ = TcHmiAsyncLogger.Send(Severity.Error, "ERROR_INIT", ex.Message);
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        private void ReadCurrentCertificate()
        {
            // get the current certificate via http request

            string serverDomain = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context,
                CfgData + TcHmiApplication.PathElementSeparator + CfgDomain);
            var serverUrl = $"http://{serverDomain}";

            if (string.IsNullOrEmpty(serverDomain))
            {
                _ = TcHmiAsyncLogger.Send(Severity.Warning, "ERROR_GET_CERTIFICATE_EMPTY_DOMAIN");
                return;
            }

            string serverId = null;
            using (var client = new HttpClient())
            {
                var error = "";
                HttpResponseMessage response = null;

                try
                {
                    response = client.Send(new HttpRequestMessage(HttpMethod.Get, serverUrl));
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                if (response == null)
                {
                    _ = TcHmiAsyncLogger.Send(Severity.Warning, "ERROR_GET_CERTIFICATE", serverUrl, error);
                    return;
                }

                var setCookieArgs = response.Headers.SingleOrDefault(h => h.Key == "Set-Cookie").Value;

                foreach (var setCookieArg in setCookieArgs)
                {
                    if (!string.IsNullOrEmpty(setCookieArg))
                    {
                        var cookies = setCookieArg.Replace(" ", "").Split(";");

                        // structure of the server's session cookie: sessionId-{serverId}={sessionId}
                        foreach (var cookie in cookies)
                        {
                            if (cookie.StartsWith("sessionId-"))
                            {
                                serverId = cookie.Replace("sessionId-", "");
                                break;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(serverId))
            {
                _ = TcHmiAsyncLogger.Send(Severity.Info, "ERROR_GET_CERTIFICATE_SERVER_ID");
                return;
            }

            var container = new CookieContainer();
            container.Add(new Uri(serverUrl), new Cookie("sessionId", TcHmiApplication.Context.Session.Id));

            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                CookieContainer = container
            };
            using (var client = new HttpClient(handler))
            {
                HttpResponseMessage response = null;

                try
                {
                    response = client.Send(new HttpRequestMessage(HttpMethod.Get, serverUrl + "/ExportCertificate"));

                    if (response is null)
                    {
                        throw new InvalidOperationException("HTTP response cannot be null.");
                    }

                    using var contentReader = new StreamReader(response.Content.ReadAsStream());
                    _currentCertificate = new X509Certificate2(Encoding.ASCII.GetBytes(contentReader.ReadToEnd()));
                }
                catch (Exception ex)
                {
                    _ = TcHmiAsyncLogger.Send(Severity.Error, "ERROR_READ_CERTIFICATE", serverUrl + "/ExportCertificate",
                        ex.Message);
                }
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
                    _intervalPath = CfgInterval;
                    break;
                case LetsEncryptStagingV2:
                    _accountKeyFile = "account-key-staging-v2.pem";
                    uri = WellKnownServers.LetsEncryptStagingV2;
                    _intervalPath = CfgIntervalStaging;
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

                var contacts = new List<string>();
                var mails = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context,
                    CfgData + TcHmiApplication.PathElementSeparator + CfgContacts);

                foreach (Value mail in mails)
                {
                    contacts.Add("mailto:" + HttpUtility.UrlEncode(mail.GetString()));
                }

                _ = _acmeContext.NewAccount(contacts, true);
                var newKey = _acmeContext.AccountKey.ToPem();
                File.WriteAllText(_accountKeyFile, newKey);
            }
        }

        private double GetCurrentRestInterval()
        {
            TimeSpan currentDuration =
                TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, _intervalPath);
            var expirationDate = DateTime.Now;

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

            if (!CheckServerConfiguration())
            {
                StopCertificateGeneration();
                return;
            }

            if (_currentCertificate == null)
            {
                ReadCurrentCertificate();
            }

            var restTime = GetCurrentRestInterval();

            if (_currentCertificate == null || restTime <= 0)
            {
                try
                {
                    GenerateCertificate();
                }
                catch (Exception e)
                {
                    _ = TcHmiAsyncLogger.Send(Severity.Error, "ERROR_CREATE_CERTIFICATE", e.Message);
                    StopCertificateGeneration();
                    return;
                }
            }

            restTime = GetCurrentRestInterval();

            if (_certGenerationTimer == null)
            {
                _certGenerationTimer = new Timer(restTime);
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

            var serverContext = TcHmiApplication.Context;
            serverContext.Domain = "TcHmiSrv";

            var evt = new Event(serverContext, "CONFIGURATION_HINT") { TimeReceived = DateTime.Now };
            var payload = new Alarm(TcHmiApplication.Context, msg)
            {
                Severity = Severity.Warning,
                Id = id,
                ConfirmationState = reset ? AlarmConfirmationState.Reset : AlarmConfirmationState.Wait
            };
            evt.Payload = payload;

            _ = TcHmiApplication.AsyncHost.SendAsync(serverContext, evt);
        }

        /// <summary>
        ///     Generate new certificate with Let's Encrypt api.
        /// </summary>
        /// <returns>ErrorValue</returns>
        private void GenerateCertificate()
        {
            _ = TcHmiAsyncLogger.Send(Severity.Info, "INFO_GENERATE_CERTIFICATE");

            string domain = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context,
                CfgData + TcHmiApplication.PathElementSeparator + CfgDomain);

            if (_currentCertificate != null && _currentKey != null)
            {
                _ = _acmeContext.RevokeCertificate(_currentCertificate.RawData, RevocationReason.KeyCompromise,
                    _currentKey);
            }
            else if (_currentCertificate != null)
            {
                _ = TcHmiAsyncLogger.Send(Severity.Info, "ERROR_REVOKE_CERTIFICATE");
            }

            var order = _acmeContext.NewOrder(new[] { domain }).GetAwaiter().GetResult();
            var authorization = order.Authorizations().GetAwaiter().GetResult().First();

            // handle Let's Encrypt challenges
            if (domain.Contains("*"))
            {
                // DNS challenge is not supported yet
                throw new TcHmiException("Domain should not be wildcard", ErrorValue.HMI_E_FAIL);
            }
            // handle acme http challenge

            if (Directory.Exists(ChallengeFilePath))
            {
                Directory.Delete(ChallengeFilePath, true);
            }

            var challenge = authorization.Http().GetAwaiter().GetResult();
            _ = Directory.CreateDirectory(ChallengeFilePath);
            File.WriteAllText(ChallengeFilePath + challenge.Token, challenge.KeyAuthz);

            var challengeValidator = challenge.Validate().GetAwaiter().GetResult();
            var maxAttempts = 5;

            while (challengeValidator.Status == ChallengeStatus.Pending && maxAttempts-- >= 0)
            {
                challengeValidator = challenge.Resource().GetAwaiter().GetResult();
            }

            if (challengeValidator.Status == ChallengeStatus.Invalid)
            {
                if (challengeValidator.Error.Status == HttpStatusCode.Unauthorized)
                {
                    ConfigurationHint(HintNoChallengeFileAccess, HintIdNoChallengeFileAccess);
                }

                _ = TcHmiAsyncLogger.Send(Severity.Error, "INFO_CHALLENGE_ERROR", challengeValidator.Error.Detail);
            }
            else if (challengeValidator.Status != ChallengeStatus.Valid)
            {
                _ = TcHmiAsyncLogger.Send(Severity.Error, "INFO_CHALLENGE_STATUS",
                    challengeValidator.Status.ToString());
            }

            ConfigurationHint(HintNoChallengeFileAccess, HintIdNoChallengeFileAccess, true);

            // generate certificate from order
            var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var infos = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context,
                CfgData + TcHmiApplication.PathElementSeparator + CfgCertificateInformation);
            var csrInfo = new CsrInfo
            {
                CountryName = infos[CfgCountryName],
                State = infos[CfgState],
                Locality = infos[CfgLocality],
                Organization = infos[CfgOrganization]
            };

            var certChain = order.Generate(csrInfo, certKey).GetAwaiter().GetResult();

            // set generated certificate in server config
            var c = TcHmiApplication.Context;
            c.Domain = "TcHmiSrv";

            var cmdGroup = new CommandGroup("TcHmiSrv",
                new List<Command>
                {
                    new Command(TcHmiApplication.JoinPath("TcHmiSrv.Config", "CERTIFICATE"))
                    {
                        WriteValue = new Value(Encoding.ASCII.GetBytes(certChain.Certificate.ToPem()))
                    },
                    new Command(TcHmiApplication.JoinPath("TcHmiSrv.Config", "KEY"))
                    {
                        WriteValue = new Value(Encoding.ASCII.GetBytes(certKey.ToPem()))
                    }
                });

            var err = TcHmiApplication.AsyncHost.Execute(ref c, ref cmdGroup);

            if (err != ErrorValue.HMI_SUCCESS)
            {
                throw new TcHmiException("Setting certificate and key in config failed", err);
            }

            // show "restart required"-hint
            _ = TcHmiAsyncLogger.Send(Severity.Info, "INFO_CERTIFICATE_SUCCEEDED");
            ConfigurationHint(HintRestartRequired, HintIdRestartRequired);

            _currentCertificate = new X509Certificate2(Encoding.ASCII.GetBytes(certChain.Certificate.ToPem()));
            _currentKey = certKey;
        }

        private void OnRequest(object sender, OnRequestEventArgs e)
        {
            foreach (var command in e.Commands)
            {
                if (command.Name.EndsWith("Diagnostics"))
                {
                    var information = new Value();

                    if (_currentCertificate != null)
                    {
                        var currentCertificate = new Value();
                        var valid = false;

                        try
                        {
                            currentCertificate.Add("validTo", _currentCertificate.NotAfter);
                            currentCertificate.Add("validFrom", _currentCertificate.NotBefore);
                            valid = _currentCertificate != null &&
                                    _currentCertificate.NotAfter.Subtract(DateTime.Now).TotalMilliseconds > 0;
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

        private void BeforeChange(object sender, BeforeChangeEventArgs e)
        {
            if (e.Path == CfgData + TcHmiApplication.PathElementSeparator + CfgDomain)
            {
                string domain = e.Value;

                if (!Uri.IsWellFormedUriString(domain, UriKind.RelativeOrAbsolute))
                {
                    throw new TcHmiException("Please enter a valid uri string", ErrorValue.HMI_E_INVALID_PARAMETER);
                }

                var resetHint = !domain.Contains("*");
                ConfigurationHint(HintWildcardSupport, HintIdWildcardSupport, resetHint);
            }
            else if (string.CompareOrdinal(e.Path,
                         CfgData + TcHmiApplication.PathElementSeparator + CfgContacts + "[") == 0)
            {
                string mail = e.Value;

                if (!Regex.IsMatch(mail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                        RegexOptions.IgnoreCase))
                {
                    throw new TcHmiException("Please enter a valid mail address", ErrorValue.HMI_E_INVALID_PARAMETER);
                }
            }
        }

        private void OnChange(object sender, OnChangeEventArgs e)
        {
            bool generateCert =
                TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, CfgGenerateCertificate);
            var refreshCertificate = e.Path == CfgData;

            if (refreshCertificate || e.Path == CfgInterval)
            {
                _certGenerationTimer.Stop();
                int api = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context,
                    CfgData + TcHmiApplication.PathElementSeparator + CfgApi);
                SetCurrentContext(api);

                if (refreshCertificate)
                {
                    if (_initFinished && generateCert)
                    {
                        try
                        {
                            GenerateCertificate();
                        }
                        catch (Exception ex)
                        {
                            _ = TcHmiAsyncLogger.Send(Severity.Error, "ERROR_CREATE_CERTIFICATE", ex.Message);
                            StopCertificateGeneration();
                            generateCert = false;
                        }
                    }
                    else
                    {
                        _initFinished = true;
                    }
                }

                var currentInterval = GetCurrentRestInterval();

                if (currentInterval <= 0)
                {
                    _certGenerationTimer.Interval = 5000;
                }
                else
                {
                    _certGenerationTimer.Interval = currentInterval;
                }

                if (generateCert)
                {
                    _certGenerationTimer.Start();
                }
            }
            else if (e.Path == CfgGenerateCertificate)
            {
                if (generateCert)
                {
                    ReadCurrentCertificate();
                    var interval = GetCurrentRestInterval();

                    if (interval < 0)
                    {
                        interval = 5000;
                    }

                    _certGenerationTimer.Interval = interval;
                    _certGenerationTimer.Start();
                }
                else
                {
                    _certGenerationTimer.Stop();
                    _nextCertGeneration = null;
                }
            }
            else if (e.Path.StartsWith(CfgData + TcHmiApplication.PathElementSeparator + CfgContacts))
            {
                IList<string> contacts = new List<string>();
                var mails = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context,
                    CfgData + TcHmiApplication.PathElementSeparator + CfgContacts);

                foreach (Value mail in mails)
                {
                    contacts.Add("mailto:" + mail.GetString());
                }

                try
                {
                    _ = _acmeContext.Account().GetAwaiter().GetResult().Update(contacts);
                }
                catch (Exception)
                {
                    try
                    {
                        _ = _acmeContext.NewAccount(contacts, true).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        _ = TcHmiAsyncLogger.Send(Severity.Error, "ERROR_GENERATE_ACCOUNT", ex.Message);
                    }
                }

                var newKey = _acmeContext.AccountKey.ToPem();
                File.WriteAllText(_accountKeyFile, newKey);
            }
            else if (e.Path == CfgIntervalStaging || e.Path == CfgInterval)
            {
                _certGenerationTimer.Stop();

                var interval = GetCurrentRestInterval();

                if (interval < 0)
                {
                    interval = 5000;
                }

                _certGenerationTimer.Interval = interval;
                _certGenerationTimer.Start();
            }
        }

        private bool CheckServerConfiguration()
        {
            var serverContext = TcHmiApplication.Context;
            serverContext.Domain = "TcHmiSrv";

            // ports 80 and 443 must be configured for Let's Encrypt

            var group = new CommandGroup("TcHmiSrv",
                new List<Command> { new Command(TcHmiApplication.JoinPath("TcHmiSrv.Config", "ENDPOINTS")) });

            var err = TcHmiApplication.AsyncHost.Execute(ref serverContext, ref group);

            if (err != ErrorValue.HMI_SUCCESS)
            {
                return false;
            }

            var endpointsCmd = group[0];

            if (!endpointsCmd.ReadValue.IsVector)
            {
                return false;
            }

            var port443Found = false;
            var port80Found = false;

            foreach (Value item in endpointsCmd.ReadValue)
            {
                var endpoint = item.GetString();
                port443Found |= endpoint == "https://0.0.0.0:443";
                port80Found |= endpoint == "http://0.0.0.0:80";
            }

            var portCheck = port443Found && port80Found;
            ConfigurationHint(HintWrongPortConfiguration, HintIdWrongPortConfiguration, portCheck);
            return portCheck;
        }

        private void StopCertificateGeneration()
        {
            _nextCertGeneration = null;
            _certGenerationTimer.Stop();
            _ = TcHmiApplication.AsyncHost.ReplaceConfigValue(TcHmiApplication.Context, CfgGenerateCertificate,
                new Value(false));
        }
    }
}
