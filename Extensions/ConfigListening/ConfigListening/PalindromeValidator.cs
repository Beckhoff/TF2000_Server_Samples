using System.Collections.Generic;
using System.Linq;

namespace ConfigListening
{
    public enum PalindromeType
    {
        None,
        CharacterUnit,
        SentenceOrPhrase
    }

    public static class PalindromeValidator
    {
        public static PalindromeType Validate(string candidate)
        {
            // transform to lower-case and put all relevant characters onto a stack
            var stack = new Stack<char> { };
            bool multipleWords = false;
            foreach (char c in candidate.ToLower())
            {
                if (char.IsWhiteSpace(c))
                {
                    multipleWords = true;
                }
                else if (!char.IsPunctuation(c))
                {
                    stack.Push(c);
                }
            }

            if (stack.Count > 0)
            {
                // split into two stacks. this reverses the second half.
                var stack2 = new Stack<char> { };
                foreach (var _ in Enumerable.Range(0, stack.Count / 2))
                {
                    stack2.Push(stack.Pop());
                }

                // correct for palindromes with an odd number of characters
                if (stack.Count == stack2.Count + 1)
                {
                    stack.Pop();
                }

                if (Enumerable.SequenceEqual(stack, stack2))
                {
                    return multipleWords ? PalindromeType.SentenceOrPhrase : PalindromeType.CharacterUnit;
                }
            }
            return PalindromeType.None;
        }
    }
}
