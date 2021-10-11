using System.Linq;

namespace ConfigListening
{
    public static class AnagramValidator
    {
        private static bool IsRelevant(char c)
        {
            return (!char.IsPunctuation(c)) || char.IsWhiteSpace(c);
        }

        private static char SelectKey(char c)
        {
            return c;
        }

        public static bool Validate(string text, string anagramOfText)
        {
            var a = text.ToLower().ToCharArray().Where(IsRelevant).OrderBy(SelectKey);
            var b = anagramOfText.ToLower().ToCharArray().Where(IsRelevant).OrderBy(SelectKey);
            return Enumerable.SequenceEqual(a, b);
        }
    }
}
