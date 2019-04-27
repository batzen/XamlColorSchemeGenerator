// Original code for this taken from https://www.codeproject.com/Articles/298519/Fast-Token-Replacement-in-Csharp

namespace XamlColorSchemeGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class FastReplacerSnippet
    {
        private class InnerSnippet
        {
            public InnerSnippet(FastReplacerSnippet snippet, int start, int end, int order1, int order2)
            {
                this.Snippet = snippet;
                this.Start = start;
                this.End = end;
                this.Order1 = order1;
                this.Order2 = order2;
            }

            public readonly FastReplacerSnippet Snippet;
            public readonly int Start; // Position of the snippet in parent snippet's Text.
            public readonly int End; // Position of the snippet in parent snippet's Text.
            public readonly int Order1; // Order of snippets with a same Start position in their parent.
            public readonly int Order2; // Order of snippets with a same Start position and Order1 in their parent.

            public override string ToString()
            {
                return "InnerSnippet: " + this.Snippet.Text;
            }
        }

        public readonly string Text;
        private readonly List<InnerSnippet> innerSnippets;

        public FastReplacerSnippet(string text)
        {
            this.Text = text;
            this.innerSnippets = new List<InnerSnippet>();
        }

        public override string ToString()
        {
            return "Snippet: " + this.Text;
        }

        public void Append(FastReplacerSnippet snippet)
        {
            var textLength = this.Text.Length;
            this.innerSnippets.Add(new InnerSnippet(snippet, textLength, textLength, 1, this.innerSnippets.Count));
        }

        public void Replace(int start, int end, FastReplacerSnippet snippet)
        {
            this.innerSnippets.Add(new InnerSnippet(snippet, start, end, 0, 0));
        }

        public void InsertBefore(int start, FastReplacerSnippet snippet)
        {
            this.innerSnippets.Add(new InnerSnippet(snippet, start, start, 2, this.innerSnippets.Count));
        }

        public void InsertAfter(int end, FastReplacerSnippet snippet)
        {
            this.innerSnippets.Add(new InnerSnippet(snippet, end, end, 1, this.innerSnippets.Count));
        }

        public void ToString(StringBuilder sb)
        {
            this.innerSnippets.Sort(CompareSnippets);

            var lastPosition = 0;
            foreach (var innerSnippet in this.innerSnippets)
            {
                if (innerSnippet.Start < lastPosition)
                {
                    throw new InvalidOperationException($"Internal error: Token is overlapping with a previous token. Overlapping token is from position {innerSnippet.Start} to {innerSnippet.End}, previous token ends at position {lastPosition} in snippet \"{this.Text}\".");
                }

                sb.Append(this.Text, lastPosition, innerSnippet.Start - lastPosition);
                innerSnippet.Snippet.ToString(sb);
                lastPosition = innerSnippet.End;
            }

            sb.Append(this.Text, lastPosition, this.Text.Length - lastPosition);
        }

        private static int CompareSnippets(InnerSnippet a, InnerSnippet b)
        {
            if (a == b)
            {
                return 0;
            }

            if (a.Start != b.Start)
            {
                return a.Start - b.Start;
            }

            if (a.End != b.End)
            {
                return a.End - b.End; // Disambiguation if there are inner snippets inserted before a token (they have End==Start) go before inner snippets inserted instead of a token (End>Start).
            }

            if (a.Order1 != b.Order1)
            {
                return a.Order1 - b.Order1;
            }

            if (a.Order2 != b.Order2)
            {
                return a.Order2 - b.Order2;
            }

            throw new InvalidOperationException($"Internal error: Two snippets have ambiguous order. At position from {a.Start} to {a.End}, order1 is {a.Order1}, order2 is {a.Order2}. First snippet is \"{a.Snippet.Text}\", second snippet is \"{b.Snippet.Text}\".");
        }

        public int GetLength()
        {
            var len = this.Text.Length;
            foreach (var innerSnippet in this.innerSnippets)
            {
                len -= innerSnippet.End - innerSnippet.Start;
                len += innerSnippet.Snippet.GetLength();
            }

            return len;
        }
    }
}