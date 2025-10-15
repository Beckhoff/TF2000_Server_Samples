using TcHmiSrv.Core;
using TcHmiSrv.Core.General;

[assembly: DoNotParallelize]

namespace CSharpRandomValueUnitTests
{
    [TestClass]
    public class CSharpRandomValueUnitTests
    {
        private const string DefaultDomain = "CSharpRandomValue";
        private const string CfgMaxRandom = "maxRandom";

        private static MockedCSharpRandomValue s_serverExtension;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            s_serverExtension = new MockedCSharpRandomValue(context, DefaultDomain);
            Assert.AreEqual(ErrorValue.HMI_SUCCESS, s_serverExtension.Init());
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(100)]
        public void OnRequestValidTest(int maxRandom)
        {
            s_serverExtension.MaxRandom = maxRandom;

            var context = new Context { Domain = DefaultDomain };
            var requestCommand = new Command();
            ((ICommandMapper)requestCommand).SetMapping("RandomValue");
            var requestCommands = new CommandGroup { requestCommand };

            s_serverExtension.OnRequest(context, requestCommands);
            Assert.AreEqual(1, requestCommands.Count);

            var responseCommand = requestCommands[0];
            Assert.IsNotNull(responseCommand);

            var readValue = responseCommand.ReadValue;
            Assert.IsNotNull(readValue);
            Assert.AreEqual(ValueType.Int32, readValue.Type);

            var randomValue = readValue.GetInt32();
            Assert.IsLessThan(s_serverExtension.MaxRandom, randomValue);
            Assert.IsGreaterThanOrEqualTo(0, randomValue);
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(-42)]
        public void OnRequestInvalidTest(int maxRandom)
        {
            s_serverExtension.MaxRandom = maxRandom;

            var context = new Context { Domain = DefaultDomain };
            var requestCommand = new Command();
            ((ICommandMapper)requestCommand).SetMapping("RandomValue");
            var requestCommands = new CommandGroup { requestCommand };

            s_serverExtension.OnRequest(context, requestCommands);
            Assert.AreEqual(1, requestCommands.Count);

            var responseCommand = requestCommands[0];
            Assert.IsNotNull(responseCommand);
            Assert.AreEqual<uint>(1, responseCommand.ExtensionResult);
            Assert.Contains(
                "Calling command \"RandomValue\" failed! Additional information: System.ArgumentOutOfRangeException",
                responseCommand.ResultString);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(100)]
        public void BeforeChangeValidTest(int maxRandom)
        {
            var context = new Context { Domain = DefaultDomain };
            s_serverExtension.BeforeChange(context, CfgMaxRandom, maxRandom, null);
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(-42)]
        public void BeforeChangeInvalidTest(int maxRandom)
        {
            var context = new Context { Domain = DefaultDomain };
            var ex = Assert.Throws<TcHmiException>(() =>
                s_serverExtension.BeforeChange(context, CfgMaxRandom, maxRandom, null));
            Assert.Contains("Max random value must not be less than zero.", ex.Message);
            Assert.AreEqual(ErrorValue.HMI_E_INVALID_PARAMETER, ex.Result);
        }
    }
}
