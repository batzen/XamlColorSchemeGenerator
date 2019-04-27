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
            public FastReplacerSnippet Snippet;
            public int Start; // Position of the snippet in parent snippet's Text.
            public int End; // Position of the snippet in parent snippet's Text.
            public int Order1; // Order of snippets with a same Start position in their parent.
            public int Order2; // Order of snippets with a same Start position and Order1 in their parent.

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
            this.innerSnippets.Add(new InnerSnippet
            {
                Snippet = snippet,
                Start = this.Text.Length,
                End = this.Text.Length,
                Order1 = 1,
                Order2 = this.innerSnippets.Count
            });
        }

        public void Replace(int start, int end, FastReplacerSnippet snippet)
        {
            this.innerSnippets.Add(new InnerSnippet
            {
                Snippet = snippet,
                Start = start,
                End = end,
                Order1 = 0,
                Order2 = 0
            });
        }

        public void InsertBefore(int start, FastReplacerSnippet snippet)
        {
            this.innerSnippets.Add(new InnerSnippet
            {
                Snippet = snippet,
                Start = start,
                End = start,
                Order1 = 2,
                Order2 = this.innerSnippets.Count
            });
        }

        public void InsertAfter(int end, FastReplacerSnippet snippet)
        {
            this.innerSnippets.Add(new InnerSnippet
            {
                Snippet = snippet,
                Start = end,
                End = end,
                Order1 = 1,
                Order2 = this.innerSnippets.Count
            });
        }

        public void ToString(StringBuilder sb)
        {
            this.innerSnippets.Sort(delegate(InnerSnippet a, InnerSnippet b)
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
            });

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