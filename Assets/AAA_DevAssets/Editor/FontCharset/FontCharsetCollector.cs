using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Lokas.Editor.FontCharset
{
    public static class FontCharsetCollector
    {
        private static readonly HashSet<string> Tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "b", "i", "u", "s", "strikethrough", "color", "alpha", "size", "font", "material", "mark", "link",
            "align", "indent", "line-indent", "line-height", "margin", "margin-left", "margin-right", "cspace", "mspace",
            "pos", "space", "voffset", "width", "nobr", "page", "rotate", "style", "sub", "sup", "sprite",
            "uppercase", "lowercase", "allcaps", "smallcaps", "font-weight", "gradient"
        };

        public static IEnumerable<uint> CodePoints(string text)
        {
            for (int i = 0; i < (text ?? "").Length; i++)
            {
                int start = i;
                uint code;
                if (char.IsHighSurrogate(text[i]))
                {
                    if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1])) throw new FormatException("文本包含不完整的 Unicode 代理对。");
                    code = (uint)char.ConvertToUtf32(text[i], text[++i]);
                }
                else if (char.IsLowSurrogate(text[i])) throw new FormatException("文本包含不完整的 Unicode 代理对。");
                else code = text[i];
                var category = CharUnicodeInfo.GetUnicodeCategory(text, start);
                if (category != UnicodeCategory.Control && code != 0xFEFF) yield return code;
            }
        }

        public static string Text(IEnumerable<uint> codes) => string.Concat(codes.Select(c => char.ConvertFromUtf32((int)c)));
        public static string Describe(IEnumerable<uint> codes) => string.Join(" ", codes.Select(c => Text(new[] { c }) + " (U+" + c.ToString("X4") + ")"));

        // Strip only recognized TMP markup. noparse contents and escaped format braces remain literal.
        public static string VisibleText(string value)
        {
            var output = new StringBuilder();
            bool noParse = false;
            bool upper = false, lower = false;
            for (int i = 0; i < value.Length;)
            {
                if (value[i] == '<')
                {
                    int end = value.IndexOf('>', i + 1);
                    if (end >= 0)
                    {
                        string token = value.Substring(i + 1, end - i - 1);
                        if (token.Equals("/noparse", StringComparison.OrdinalIgnoreCase)) { noParse = false; i = end + 1; continue; }
                        if (!noParse)
                        {
                            if (token.Equals("noparse", StringComparison.OrdinalIgnoreCase)) { noParse = true; i = end + 1; continue; }
                            var name = Regex.Match(token, @"^/?([a-zA-Z-]+)").Groups[1].Value;
                            if (name.Equals("br", StringComparison.OrdinalIgnoreCase)) { output.Append('\n'); i = end + 1; continue; }
                            if (name.Equals("style", StringComparison.OrdinalIgnoreCase))
                                throw new FormatException("<style> 可能展开额外文字，请先展开样式或通过明确的文案来源处理。");
                            if (name.Equals("uppercase", StringComparison.OrdinalIgnoreCase) || name.Equals("allcaps", StringComparison.OrdinalIgnoreCase) || name.Equals("smallcaps", StringComparison.OrdinalIgnoreCase)) upper = true;
                            if (name.Equals("lowercase", StringComparison.OrdinalIgnoreCase)) lower = true;
                            if (Tags.Contains(name) || Regex.IsMatch(token, @"^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$")) { i = end + 1; continue; }
                        }
                    }
                }
                // Formatting occurs before TMP parsing, including inside noparse.
                if (i + 1 < value.Length && ((value[i] == '{' && value[i + 1] == '{') || (value[i] == '}' && value[i + 1] == '}')))
                { output.Append(value[i]); i += 2; continue; }
                if (value[i] == '{')
                {
                    var match = Regex.Match(value.Substring(i), @"^\{\d+(?:\s*,\s*-?\d+)?(?::[^{}]*)?\}");
                    if (match.Success) { i += match.Length; continue; }
                }
                output.Append(value[i++]);
            }
            string text = output.ToString();
            // Conservatively retain originals and case variants; never remove required characters.
            return text + (upper ? text.ToUpperInvariant() : "") + (lower ? text.ToLowerInvariant() : "");
        }

        public static List<KeyValuePair<string, string>> ReadXml(string xml, string language, string path)
        {
            using (var reader = XmlReader.Create(new System.IO.StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                var document = XDocument.Load(reader);
                var dictionaries = document.Root?.Elements("Dictionary").Where(d => (string)d.Attribute("Language") == language).ToArray();
                if (document.Root?.Name != "Dictionaries" || dictionaries == null || dictionaries.Length != 1)
                    throw new FormatException(path + " 必须包含唯一的 Dictionary Language=" + language);
                var seen = new HashSet<string>();
                var result = new List<KeyValuePair<string, string>>();
                foreach (var node in dictionaries[0].Elements("String"))
                {
                    string key = (string)node.Attribute("Key"), value = (string)node.Attribute("Value");
                    if (string.IsNullOrEmpty(key) || value == null || !seen.Add(key)) throw new FormatException(path + " 缺少 Key/Value 或重复 Key: " + key);
                    result.Add(new KeyValuePair<string, string>(path + " / " + key, VisibleText(value)));
                }
                return result;
            }
        }
    }
}
