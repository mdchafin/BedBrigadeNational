﻿using System.Text;
using System.Text.RegularExpressions;

namespace BedBrigade.Common
{
    public static class StringUtil
    {
        private const string hrefExpression = @"<a[^>]*>.*<\/a>";
        private static Regex _hrefRegex = new Regex(hrefExpression, RegexOptions.Compiled);
        private const string javaScriptExpression = @"<a.+javascript[^>]+>[^>]+>";
        private static Regex _javaScriptRegex = new Regex(javaScriptExpression, RegexOptions.Compiled);

        public static string RestoreHrefWithJavaScript(string? original, string? updated)
        {
            original ??= string.Empty;
            updated ??= string.Empty;

            var originalMatches = _javaScriptRegex.Matches(original);

            if (originalMatches.Count == 0)
                return updated;
            
            var updatedLinks = _hrefRegex.Matches(updated);

            var sb = new StringBuilder(updated, updated.Length*2);
            foreach (Match originalMatch in originalMatches)
            {
                var originalLink = originalMatch.Value;
                var originalHrefText = GetBetweenTags(originalLink, ">", "</a>");

                foreach (Match updatedLinkMatch in updatedLinks)
                {
                    var updatedLink = updatedLinkMatch.Value;
                    if (updatedLink.Contains($">{originalHrefText}</a>"))
                    {
                        sb.Replace(updatedLink, originalLink);
                        break;
                    }
                }
            }

            return sb.ToString();
        }

        public static string GetTag(string searchText, string startTag, string endTag)
        {
            if (String.IsNullOrEmpty(searchText))
                return searchText;

            int startTagPos = searchText.IndexOf(startTag, StringComparison.Ordinal);

            if (startTagPos < 0)
                return string.Empty;

            int endTagPos = searchText.IndexOf(endTag, startTagPos + startTag.Length, StringComparison.Ordinal);

            if (endTagPos < 0)
                return string.Empty;

            return searchText.Substring(startTagPos, endTagPos - startTagPos + endTag.Length);
        }

        public static string GetBetweenTags(string searchText, string startTag, string endTag)
        {
            if (String.IsNullOrEmpty(searchText))
                return searchText;

            int startTagPos = searchText.IndexOf(startTag, StringComparison.Ordinal);

            if (startTagPos < 0)
                return string.Empty;

            int endTagPos = searchText.IndexOf(endTag, startTagPos + startTag.Length, StringComparison.Ordinal);

            if (endTagPos < 0)
                return string.Empty;

            return searchText.Substring(startTagPos + startTag.Length, endTagPos - startTagPos - startTag.Length);
        }

        /// <summary>
        /// Insert spaces into a string 
        /// </summary>
        /// <example>
        /// OrderDetails = Order Details
        /// 10Net30 = 10 Net 30
        /// FTPHost = FTP Host
        /// </example> 
        /// <param name="input"></param>
        /// <returns></returns>
        public static string InsertSpaces(string input)
        {
            bool isSpace = false;
            bool isUpperOrNumber = false;
            bool isLower = false;
            bool isLastUpper = true;
            bool isNextCharLower = false;

            if (String.IsNullOrEmpty(input))
                return string.Empty;

            StringBuilder sb = new StringBuilder(input.Length + (int)(input.Length / 2));

            //Replace underline with spaces
            input = input.Replace("_", " ");
            input = input.Replace("-", " ");
            input = input.Replace("  ", " ");

            //Trim any spaces
            input = input.Trim();

            char[] chars = input.ToCharArray();

            sb.Append(chars[0]);

            for (int i = 1; i < chars.Length; i++)
            {
                isUpperOrNumber = (chars[i] >= 'A' && chars[i] <= 'Z') || (chars[i] >= '0' && chars[i] <= '9');
                isNextCharLower = i < chars.Length - 1 && (chars[i + 1] >= 'a' && chars[i + 1] <= 'z');
                isSpace = chars[i] == ' ';
                isLower = (chars[i] >= 'a' && chars[i] <= 'z');

                //There was a space already added
                if (isSpace)
                {
                }
                //Look for upper case characters that have lower case characters before
                //Or upper case characters where the next character is lower
                else if ((isUpperOrNumber && isLastUpper == false)
                    || (isUpperOrNumber && isNextCharLower && isLastUpper == true))
                {
                    sb.Append(' ');
                    isLastUpper = true;
                }
                else if (isLower)
                {
                    isLastUpper = false;
                }

                sb.Append(chars[i]);

            }

            //Replace double spaces
            sb.Replace("  ", " ");

            return sb.ToString();
        }
    }
}
