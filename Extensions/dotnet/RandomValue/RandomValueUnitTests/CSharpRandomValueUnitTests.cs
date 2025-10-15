using TcHmiSrv.Core;

[assembly: DoNotParallelize]

namespace CSharpRandomValueUnitTests
{
    [TestClass]
    public class CSharpRandomValueUnitTests
    {
        private const string DefaultDomain = "CSharpRandomValue";

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
    }
}
