﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Internal;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace BedBrigade.SpeakIt.Tests
{
    [TestFixture]
    public class LocalizationSeedingSetup
    {
        private ParseLogic _parseLogic = new ParseLogic();
        private TranslateLogic _translateLogic = new TranslateLogic(null);
        private Dictionary<string, string> _localizableStrings = new Dictionary<string, string>();

        [Test, Ignore("Seeded National and Grove City. Keep for Polaris.")]
        public void Setup()
        {
            FillLocalizableStrings();
            CreateYamlFile();
        }

        private void CreateYamlFile()
        {
            string filePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Data", "Data", "Seeding", "SeedTranslations", "en-US.yml");
            var newSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            string newContent = newSerializer.Serialize(_localizableStrings);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(newContent);
            }
        }

        private void FillLocalizableStrings()
        {
            string htmlPath =
                Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Data", "Data", "Seeding", "SeedHtml");

            string[] files = Directory.GetFiles(htmlPath, "*.html");
            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                List<ParseResult> parseResults = _parseLogic.GetLocalizableStringsInText(text);

                foreach (ParseResult parseResult in parseResults)
                {
                    string key = _translateLogic.ComputeSHA512Hash(parseResult.LocalizableString);
                    if (!_localizableStrings.ContainsKey(key))
                    {
                        _localizableStrings.Add(key, parseResult.LocalizableString);
                    }
                    else
                    {
                        if (_localizableStrings[key] != parseResult.LocalizableString)
                        {
                            throw new ArgumentException(
                                $"Duplicate key found with different values. Key: {key}. Value1: {_localizableStrings[key]}. Value2: {parseResult.LocalizableString}");
                        }
                    }
                }
            }
        }


    }
}