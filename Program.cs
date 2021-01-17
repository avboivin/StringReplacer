using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace stringReplacer
{
    class Program
    {
        public static Dictionary<string, string> ReplacementRules = new Dictionary<string, string>()
        {
            {"John","Freddy" },
            {"John walks","Freddy runs" },
            {"brown dog","gray dog" },
            {"dog","cat" },
            {"- not -", "(not)" },
            {"(","" },
            {")","" },
            {"whenever",  "sometimes, when"},
            {"raining", "snowing" },
            {"his", "many" }
        };

        static void Main(string[] args)
        {
            string Input = "John walks his brown dog whenever it's - not - raining";
            string ExpectedOutput = "Freddy walks many gray dog sometimes, when it's (not) snowing";

            //This doesn't work, rules get applied twice
            string SimpleStringReplaceOutput = SimpleStringReplace(Input, ReplacementRules);
            ValidateReplacement("SimpleStringReplace", SimpleStringReplaceOutput, ExpectedOutput);

            //Exactly the same thing as simple string replace
            string SimpleRegexReplaceOutput = SimpleRegexReplace(Input, ReplacementRules);
            ValidateReplacement("SimpleRegexReplace", SimpleRegexReplaceOutput, ExpectedOutput);

            //Trying to add word boundaries
            string SimpleRegexReplaceV2Output = SimpleRegexReplaceV2(Input, ReplacementRules);
            ValidateReplacement("SimpleRegexReplaceV2", SimpleRegexReplaceV2Output, ExpectedOutput);

            //Trying a different regex and reordering replacements so it works down from largest to smallest
            //Regex here just doesn't work and the "(" replacement matches and removes an entire word
            string RegexReplaceV2WithOrderedDeltaOutput = RegexReplaceV2WithOrderedDelta(Input, ReplacementRules);
            ValidateReplacement("RegexReplaceV2WithOrderedDelta", RegexReplaceV2WithOrderedDeltaOutput, ExpectedOutput);

            string TestReplaceOutput = TestReplace(Input, ReplacementRules);
            ValidateReplacement("TestReplace", TestReplaceOutput, ExpectedOutput);
        }

        public static void ValidateReplacement(string MethodName, string Actual, string Expected)
        {
            Console.Write($"{MethodName} : ");

            if (Expected != Actual)
                Console.WriteLine("String replacement doesn't work");
            else
                Console.WriteLine("It works");

            Console.WriteLine($"Expected : {Expected}");
            Console.WriteLine($"Actual   : {Actual} \n\n");

        }

        public static string SimpleStringReplace(string input, Dictionary<string, string> ReplacementRules)
        {
            foreach (var replacementRule in ReplacementRules)
            {
                input = input.Replace(replacementRule.Key, replacementRule.Value);
            }

            return input;
        }


        public static string SimpleRegexReplace(string input, Dictionary<string, string> ReplacementRules)
        {

            foreach (var replacementRule in ReplacementRules)
            {
                input = Regex.Replace(input, Regex.Escape(replacementRule.Key), replacementRule.Value);
            }

            return input;
        }

        public static string SimpleRegexReplaceV2(string input, Dictionary<string, string> ReplacementRules)
        {
            foreach (var replacementRule in ReplacementRules)
            {
                input = Regex.Replace(input, @"\b" + Regex.Escape(replacementRule.Key) + @"\b", replacementRule.Value);
            }

            return input;
        }

        public static string RegexReplaceV2WithOrderedDelta(string input, Dictionary<string, string> ReplacementRules)
        {
            List<KeyValuePair<Regex, string>> rules = new List<KeyValuePair<Regex, string>>();
            Dictionary<Regex, int> lengthFromToDelta = new Dictionary<Regex, int>();

            foreach (var replacementRule in ReplacementRules)
            {
                Regex MatchingRule = new Regex(@"[^$\s]*" + Regex.Escape(replacementRule.Key) + @"[^$\s]*", RegexOptions.None);
                rules.Add(new KeyValuePair<Regex, string>(MatchingRule, replacementRule.Value));
                int delta = replacementRule.Key.Length - replacementRule.Value.Length;
                lengthFromToDelta[MatchingRule] = delta;
            }

            //Order the rules from the most replacing rule to the least replacing rule in term of length delta differences
            rules = rules.OrderByDescending(keyValuePair => lengthFromToDelta[keyValuePair.Key]).ToList();

            foreach (var rule in rules)
            {
                Regex pattern = rule.Key;
                string to = rule.Value;

                input = pattern.Replace(input, to);
            }

            return input;
        }
        public static string TestReplace(string input, Dictionary<string, string> ReplacementRules)
        {
            HashSet<int> LockedStringSegment = new HashSet<int>();

            foreach (var rule in ReplacementRules)
            {
                string from = Regex.Escape(rule.Key);
                string to = rule.Value;
                var match = Regex.Match(input, from);
                if (match.Success)
                {
                    List<int> AffectedCharacterPositions = Enumerable.Range(match.Index, match.Length).ToList();

                    if (!AffectedCharacterPositions.Any(x => LockedStringSegment.Contains(x)))
                    {
                        input = Regex.Replace(input, from, to);
                        int LengthDelta = to.Length - rule.Key.Length;

                        LockedStringSegment
                            .Where(x => x > match.Index + rule.Key.Length).OrderByDescending(x => x).ToList()
                            .ForEach(x =>
                        {
                            //We shuffle the locked character's place depending on the replacement delta.
                            LockedStringSegment.Remove(x);
                            LockedStringSegment.Add(x + LengthDelta);
                        });

                        //Add all the new locked character's position to the hashset.
                        Enumerable.Range(match.Index, to.Length).ToList().ForEach(x => LockedStringSegment.Add(x));

                    }
                }
            }

            return input;
        }

    }
}
