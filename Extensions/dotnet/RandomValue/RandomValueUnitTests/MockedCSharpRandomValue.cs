using System;
using TcHmiSrv.Core;

namespace CSharpRandomValueUnitTests
{
    internal sealed class MockedCSharpRandomValue : CSharpRandomValue.CSharpRandomValue
    {
        private readonly TestContext _context;
        private readonly string _domain;

        public MockedCSharpRandomValue(TestContext context, string domain)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            _domain = domain;
        }

        public int MaxRandom { get; set; } = 10;

        private string GetFormatString(string name)
        {
            return name switch
            {
                "errorInit" => "Initializing extension \"CSharpRandomValue\" failed. Additional information: {0}",
                "errorCallCommand" => "Calling command \"{0}\" failed! Additional information: {1}",
                _ => throw new ArgumentException($"Unknown name: {name}", nameof(name))
            };
        }

        protected override void RegisterListeners()
        {
            // Listeners can only be registered if the server extension is loaded by the TwinCAT HMI Server
        }

        protected override Value GetConfigValue(string path)
        {
            return path switch
            {
                "maxRandom" => MaxRandom,
                _ => throw new ArgumentException($"Unknown path: {path}", nameof(path))
            };
        }

        protected override string Localize(Context context, string name, params string[] parameters)
        {
            Assert.IsNotNull(context);
            Assert.AreEqual(_domain, context.Domain);

            var formatString = GetFormatString(name);
            return string.Format(formatString, parameters);
        }

        protected override ErrorValue Send(Severity severity, string name, params string[] parameters)
        {
            var formatString = GetFormatString(name);
            var formattedString = string.Format(formatString, parameters);

            switch (severity)
            {
                case Severity.Diagnostics:
                case Severity.Verbose:
                case Severity.Info:
                    _context.WriteLine(formattedString);
                    break;
                case Severity.Warning:
                    Assert.Inconclusive(formattedString);
                    break;
                case Severity.Error:
                case Severity.Critical:
                    Assert.Fail(formattedString);
                    break;
                default:
                    throw new ArgumentException($"Unknown severity: {severity}", nameof(severity));
            }

            return ErrorValue.HMI_SUCCESS;
        }
    }
}
