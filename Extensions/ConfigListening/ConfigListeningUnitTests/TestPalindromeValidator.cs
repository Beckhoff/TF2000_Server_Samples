using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConfigListening;

namespace ConfigListeningUnitTests
{
    [TestClass]
    public class TestPalindromeValidator
    {
        [TestMethod]
        public void Invalid()
        {
            Assert.AreEqual(PalindromeType.None, PalindromeValidator.Validate("hello"));
            Assert.AreEqual(PalindromeType.None, PalindromeValidator.Validate("[]"));
            Assert.AreEqual(PalindromeType.None, PalindromeValidator.Validate("not a palindrome"));
            Assert.AreEqual(PalindromeType.None, PalindromeValidator.Validate(""));
            Assert.AreEqual(PalindromeType.None, PalindromeValidator.Validate(";.?!,"));
            Assert.AreEqual(PalindromeType.None, PalindromeValidator.Validate(";.?!, ;;\t"));
        }

        [TestMethod]
        public void CharacterUnits()
        {
            Assert.AreEqual(PalindromeType.CharacterUnit, PalindromeValidator.Validate("x"));
            Assert.AreEqual(PalindromeType.CharacterUnit, PalindromeValidator.Validate("xx"));
            Assert.AreEqual(PalindromeType.CharacterUnit, PalindromeValidator.Validate("radar"));
            Assert.AreEqual(PalindromeType.CharacterUnit, PalindromeValidator.Validate("kayak"));
            Assert.AreEqual(PalindromeType.CharacterUnit, PalindromeValidator.Validate("ReViver"), "the validator should not be case-sensitive");
        }

        [TestMethod]
        public void SentencesAndPhrases()
        {
            Assert.AreEqual(PalindromeType.SentenceOrPhrase, PalindromeValidator.Validate("my gym"));
            Assert.AreEqual(PalindromeType.SentenceOrPhrase, PalindromeValidator.Validate("no lemon, no melon"));
            Assert.AreEqual(PalindromeType.SentenceOrPhrase, PalindromeValidator.Validate("Murder for a jar of red rum"));
            Assert.AreEqual(PalindromeType.SentenceOrPhrase, PalindromeValidator.Validate("Mr. Owl ate my metal worm"));
            Assert.AreEqual(PalindromeType.SentenceOrPhrase, PalindromeValidator.Validate("Eva, can I see bees in a cave?"));
        }
    }
}
