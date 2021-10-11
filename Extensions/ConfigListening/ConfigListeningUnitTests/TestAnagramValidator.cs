using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConfigListening;

namespace ConfigListeningUnitTests
{
    [TestClass]
    public class TestAnagramValidator
    {
        [TestMethod]
        public void Invalid()
        {
            Assert.IsFalse(AnagramValidator.Validate("abc", ""));
            Assert.IsFalse(AnagramValidator.Validate("abc", "xyz"));
            Assert.IsFalse(AnagramValidator.Validate("abc", "aabc"));
            Assert.IsFalse(AnagramValidator.Validate("abc", "aac"));
        }

        [TestMethod]
        public void Valid()
        {
            Assert.IsTrue(AnagramValidator.Validate("", ""));
            Assert.IsTrue(AnagramValidator.Validate("?", "!"));
            Assert.IsTrue(AnagramValidator.Validate("a", "a"));
            Assert.IsTrue(AnagramValidator.Validate("aa", "aa"));
            Assert.IsTrue(AnagramValidator.Validate("w-ho", "ho-w"));
            Assert.IsTrue(AnagramValidator.Validate("night,", "thing,"));
            Assert.IsTrue(AnagramValidator.Validate("the Morse Code", "here come dots"));
            Assert.IsTrue(AnagramValidator.Validate("San Diego", "diagnose"));
        }
    }
}
