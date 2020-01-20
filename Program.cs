using System;
using Platform.Memory;
using Platform.Data;
using Platform.Data.Doublets;
using Platform.Data.Doublets.ResizableDirectMemory;
using Platform.Data.Doublets.Sequences.Indexes;
using Platform.Data.Doublets.Numbers.Raw;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RegexInferer
{
    class Program
    {
        static void Main(string[] args)
        {
            var constants = new LinksConstants<ulong>((1, long.MaxValue), (long.MaxValue + 1UL, ulong.MaxValue));
            using var memory = new UInt64ResizableDirectMemoryLinks(new HeapResizableDirectMemory());
            var links = memory.DecorateWithAutomaticUniquenessAndUsagesResolution();

            var addressToRawNumberConverter = new AddressToRawNumberConverter<ulong>();
            var rawNumberToAddressConverter = new RawNumberToAddressConverter<ulong>();

            var root = links.GetOrCreate(1UL, 1UL);
            var unicodeSymbolMarker = links.GetOrCreate(root, addressToRawNumberConverter.Convert(1));
            var patternRootMarker = links.GetOrCreate(root, addressToRawNumberConverter.Convert(2));

            var charToUnicodeSymbolConverter = new Platform.Data.Doublets.Unicode.CharToUnicodeSymbolConverter<ulong>(links, addressToRawNumberConverter, unicodeSymbolMarker);

            var strings = new[] { "href", "target", "rel", "media", "hreflang", "type", "sizes", "content", "name", "src", "charset", "text", "cite", "ping", "alt", "sandbox", "width", "height", "data", "value", "poster", "coords", "shape", "scope", "action", "enctype", "method", "accept", "max", "min", "pattern", "placeholder", "step", "label", "wrap", "icon", "radiogroup" };

            var patternRootMarkerArray = new[] { patternRootMarker };

            var sequences = strings.Select((s, i) => patternRootMarkerArray.Concat(BuildSequence(s, i, links, addressToRawNumberConverter, charToUnicodeSymbolConverter)).Concat(patternRootMarkerArray).ToArray()).ToArray();

            var index = new SequenceIndex<ulong>(links);

            var any = links.Constants.Any;
            var @continue = links.Constants.Continue;

            for (int i = 0; i < sequences.Length; i++)
            {
                index.Add(sequences[i]);
            }

            var chars = new Dictionary<ulong, char>();

            links.Each(linkParts =>
            {
                var link = new UInt64Link(linkParts);

                if (link.Target == unicodeSymbolMarker)
                {
                    var symbol = (char)rawNumberToAddressConverter.Convert(link.Source);
                    chars.Add(link.Index, symbol);
                    Console.WriteLine($"({link.Index}: '{symbol}'->{link.Target})");
                }
                else
                {
                    var sourceString = LinkToString(links, constants, link.Source, chars, rawNumberToAddressConverter);
                    var targetString = LinkToString(links, constants, link.Target, chars, rawNumberToAddressConverter);
                    Console.WriteLine($"({link.Index}: {sourceString}->{targetString})");
                }
                return @continue;
            }, new UInt64Link(any, any, any));

            StringBuilder sb = new StringBuilder();
            sb.Append('^');
            AppendPattern(links, constants, patternRootMarker, patternRootMarker, chars, any, @continue, sb, 0UL, rawNumberToAddressConverter);
            sb.Append('$');
            var result = sb.ToString();

            var simplificationRegex = new Regex(@"\(([a-z\?]*)\)", RegexOptions.Compiled);
            while (simplificationRegex.IsMatch(result))
            {
                result = simplificationRegex.Replace(result, "$1");
            }

            // (|t)
            // t?
            var simplificationRegex2 = new Regex(@"\(\|([a-z])\)", RegexOptions.Compiled);
            while (simplificationRegex2.IsMatch(result))
            {
                result = simplificationRegex2.Replace(result, "$1?");
            }

            // Repeat
            while (simplificationRegex.IsMatch(result))
            {
                result = simplificationRegex.Replace(result, "$1");
            }

            var regex = new Regex(result);

            for (int i = 0; i < strings.Length; i++)
            {
                if (!regex.IsMatch(strings[i]))
                {
                    Console.WriteLine($"Error: {strings[i]} does not match the pattern.");
                }
            }

            Console.WriteLine(result);

            Console.WriteLine(links.Count());
            Console.WriteLine("Hello World!");
        }

        private static ulong[] BuildSequence(string s, int i, ILinks<ulong> links, AddressToRawNumberConverter<ulong> addressToRawNumberConverter, Platform.Data.Doublets.Unicode.CharToUnicodeSymbolConverter<ulong> charToUnicodeSymbolConverter)
        {
            var result = s.Select((c, i) => BuiltCharacterPosition(links, addressToRawNumberConverter, charToUnicodeSymbolConverter, c, i)).ToArray();
            return result;
        }

        private static ulong BuiltCharacterPosition(ILinks<ulong> links, AddressToRawNumberConverter<ulong> addressToRawNumberConverter, Platform.Data.Doublets.Unicode.CharToUnicodeSymbolConverter<ulong> charToUnicodeSymbolConverter, char c, int i)
        {
            var source = addressToRawNumberConverter.Convert((ulong)(i + 10));
            var target = charToUnicodeSymbolConverter.Convert(c);
            var result = links.GetOrCreate(source, target);
            return result;
        }

        private static string LinkToString(ILinks<ulong> links, LinksConstants<ulong> constants, ulong linkAddress, Dictionary<ulong, char> chars, RawNumberToAddressConverter<ulong> rawNumberToAddressConverter)
        {
            if (chars.TryGetValue(linkAddress, out char @char))
            {
                return $"'{@char.ToString()}'";
            }
            else if (constants.IsExternalReference(linkAddress))
            {
                return rawNumberToAddressConverter.Convert(linkAddress).ToString();
            }
            else
            {
                var link = new UInt64Link(links.GetLink(linkAddress));
                if (constants.IsExternalReference(link.Source) && chars.TryGetValue(link.Target, out char targetChar))
                {
                    return $"[{rawNumberToAddressConverter.Convert(link.Source)}]'{targetChar}'";
                }
                else
                {
                    return linkAddress.ToString();
                }
            }
        }

        private static void AppendPattern(ILinks<ulong> links, LinksConstants<ulong> constants, ulong start, ulong patternMarker, Dictionary<ulong, char> chars, ulong any, ulong @continue, StringBuilder sb, ulong initialPosition, RawNumberToAddressConverter<ulong> rawNumberToAddressConverter)
        {
            sb.Append('(');
            var alternatives = 0;
            links.Each(linkParts =>
            {
                var link = new UInt64Link(linkParts);
                if (patternMarker == link.Target)
                {
                    if (alternatives > 0)
                    {
                        sb.Append('|');
                    }
                    alternatives++;
                }
                else if (!constants.IsExternalReference(link.Target))
                {
                    var charPosition = new UInt64Link(links.GetLink(link.Target));
                    if (constants.IsExternalReference(charPosition.Source) && chars.TryGetValue(charPosition.Target, out char targetSymbol))
                    {
                        var position = rawNumberToAddressConverter.Convert(charPosition.Source) - 10;
                        if (position == initialPosition)
                        {
                            if (alternatives > 0)
                            {
                                sb.Append('|');
                            }
                            sb.Append(targetSymbol);
                            AppendPattern(links, constants, link.Target, patternMarker, chars, any, @continue, sb, initialPosition + 1, rawNumberToAddressConverter);
                            alternatives++;
                        }
                    }
                }
                return @continue;
            }, new UInt64Link(any, start, any));
            sb.Append(')');
        }
    }
}
