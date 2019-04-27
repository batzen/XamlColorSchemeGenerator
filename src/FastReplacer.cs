// Original code for this taken from https://www.codeproject.com/Articles/298519/Fast-Token-Replacement-in-Csharp

namespace XamlColorSchemeGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// FastReplacer is a utility class similar to StringBuilder, with fast Replace function.
    /// FastReplacer is limited to replacing only properly formatted tokens.
    /// Use ToString() function to get the final text.
    /// </summary>
    public class FastReplacer
    {
        private readonly string tokenOpen;
        private readonly string tokenClose;

        private readonly int tokenOpenLength;
        private readonly int tokenCloseLength;

        /// <summary>
        /// All tokens that will be replaced must have same opening and closing delimiters, such as "{" and "}".
        /// </summary>
        /// <param name="tokenOpen">Opening delimiter for tokens.</param>
        /// <param name="tokenClose">Closing delimiter for tokens.</param>
        /// <param name="caseSensitive">Set caseSensitive to false to use case-insensitive search when replacing tokens.</param>
        public FastReplacer(string tokenOpen, string tokenClose, bool caseSensitive = true)
        {
            if (string.IsNullOrEmpty(tokenOpen) || string.IsNullOrEmpty(tokenClose))
            {
                throw new ArgumentException("Token must have opening and closing delimiters, such as \"{\" and \"}\".");
            }

            this.tokenOpen = tokenOpen;
            this.tokenClose = tokenClose;

            this.tokenOpenLength = this.tokenOpen.Length;
            this.tokenCloseLength = this.tokenClose.Length;

            var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.InvariantCultureIgnoreCase;
            this.occurrencesOfToken = new Dictionary<string, List<TokenOccurrence>>(stringComparer);
        }

        private readonly FastReplacerSnippet rootSnippet = new FastReplacerSnippet(string.Empty);

        private class TokenOccurrence
        {
            public FastReplacerSnippet Snippet;
            public int Start; // Position of a token in the snippet.
            public int End; // Position of a token in the snippet.
        }

        private readonly Dictionary<string, List<TokenOccurrence>> occurrencesOfToken;

        public void Append(string text)
        {
            var snippet = new FastReplacerSnippet(text);
            this.rootSnippet.Append(snippet);
            this.ExtractTokens(snippet);
        }

        /// <returns>Returns true if the token was found, false if nothing was replaced.</returns>
        public bool Replace(string token, string text)
        {
            if (this.occurrencesOfToken.TryGetValue(token, out var occurrences) 
                && occurrences.Count > 0)
            {
                this.occurrencesOfToken.Remove(token);
                var snippet = new FastReplacerSnippet(text);

                foreach (var occurrence in occurrences)
                {
                    occurrence.Snippet.Replace(occurrence.Start, occurrence.End, snippet);
                }

                this.ExtractTokens(snippet);
                return true;
            }

            return false;
        }

        /// <returns>Returns true if the token was found, false if nothing was replaced.</returns>
        public bool InsertBefore(string token, string text)
        {
            if (this.occurrencesOfToken.TryGetValue(token, out var occurrences) && occurrences.Count > 0)
            {
                var snippet = new FastReplacerSnippet(text);
                foreach (var occurrence in occurrences)
                {
                    occurrence.Snippet.InsertBefore(occurrence.Start, snippet);
                }

                this.ExtractTokens(snippet);
                return true;
            }
            return false;
        }

        /// <returns>Returns true if the token was found, false if nothing was replaced.</returns>
        public bool InsertAfter(string token, string text)
        {
            if (this.occurrencesOfToken.TryGetValue(token, out var occurrences) && occurrences.Count > 0)
            {
                var snippet = new FastReplacerSnippet(text);
                foreach (var occurrence in occurrences)
                {
                    occurrence.Snippet.InsertAfter(occurrence.End, snippet);
                }

                this.ExtractTokens(snippet);
                return true;
            }
            return false;
        }

        public bool Contains(string token)
        {
            if (this.occurrencesOfToken.TryGetValue(token, out var occurrences))
            {
                return occurrences.Count > 0;
            }

            return false;
        }

        private void ExtractTokens(FastReplacerSnippet snippet)
        {
            var last = 0;
            while (last < snippet.Text.Length)
            {
                // Find next token position in snippet.Text:
                var start = snippet.Text.IndexOf(this.tokenOpen, last, StringComparison.InvariantCultureIgnoreCase);
                if (start == -1)
                {
                    return;
                }

                var end = snippet.Text.IndexOf(this.tokenClose, start + this.tokenOpenLength, StringComparison.InvariantCultureIgnoreCase);
                if (end == -1)
                {
                    throw new ArgumentException($"Token is opened but not closed in text \"{snippet.Text}\".");
                }

                var eol = snippet.Text.IndexOf('\n', start + this.tokenOpenLength);
                if (eol != -1 && eol < end)
                {
                    last = eol + 1;
                    continue;
                }

                // Take the token from snippet.Text:
                end += this.tokenCloseLength;
                var token = snippet.Text.Substring(start, end - start);
                var context = snippet.Text;
                this.ValidateToken(token, context, true);

                // Add the token to the dictionary:
                var tokenOccurrence = new TokenOccurrence { Snippet = snippet, Start = start, End = end };
                var tokenKey = token.Substring(this.tokenOpenLength, token.Length - this.tokenOpenLength - this.tokenCloseLength);
                if (this.occurrencesOfToken.TryGetValue(tokenKey, out var occurrences))
                {
                    occurrences.Add(tokenOccurrence);
                }
                else
                {
                    this.occurrencesOfToken.Add(tokenKey, new List<TokenOccurrence> { tokenOccurrence });
                }

                last = end;
            }
        }

        private void ValidateToken(string token, string context, bool alreadyValidatedStartAndEnd)
        {
            if (token.Length == this.tokenOpenLength + this.tokenCloseLength)
            {
                throw new ArgumentException($"Token has no body. Used with text \"{context}\".");
            }

            if (token.Contains("\n"))
            {
                throw new ArgumentException($"Unexpected end-of-line within a token. Used with text \"{context}\".");
            }

            if (token.IndexOf(this.tokenOpen, this.tokenOpenLength, StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                throw new ArgumentException($"Next token is opened before a previous token was closed in token \"{token}\". Used with text \"{context}\".");
            }
        }

        public override string ToString()
        {
            var totalTextLength = this.rootSnippet.GetLength();
            var sb = new StringBuilder(totalTextLength);
            this.rootSnippet.ToString(sb);
            if (sb.Length != totalTextLength)
            {
                throw new InvalidOperationException($"Internal error: Calculated total text length ({totalTextLength}) is different from actual ({sb.Length}).");
            }

            return sb.ToString();
        }
    }
}