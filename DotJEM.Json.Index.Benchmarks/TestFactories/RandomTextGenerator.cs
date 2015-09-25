using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DotJEM.Json.Index.Benchmarks.TestFactories
{
    public class RandomTextGenerator
    {
        private readonly string[] texts = "Childharold,Decameron,Faust,Inderfremde,Lebateauivre,Lemasque,Loremipsum,Nagyonfaj,Omagyar,Robinsonokruso,Theraven,Tierrayluna".Split(',');

        public string RandomText()
        {
            return texts.RandomItem();
        }

        public string Paragraph(string @from, int count = 20)
        {
            return Open(from).RandomItems(count).Aggregate((s, s1) => s + " " + s1);
        }

        public string Word(string @from, int minLength = 2)
        {
            return Open(from).Where(w => w.Length >= minLength).RandomItem();
        }

        private IEnumerable<string> Open(string @from)
        {
            if(!texts.Contains(@from))
                throw new ArgumentException(string.Format("The text '{0}' was unknown.", @from),"from");

            Debug.Assert(LoremIpsums.ResourceManager != null, "LoremIpsums.ResourceManager != null");

            string text = LoremIpsums.ResourceManager.GetString(@from, LoremIpsums.Culture);
            Debug.Assert(text != null, "text != null");

            return text.Split(new []{' '},StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] Words(string @from, int minLength = 2, int count = 20)
        {
            HashSet<string> unique = new HashSet<string>(Open(from).Where(w => w.Length >= minLength));
            return Enumerable.Repeat("", count)
                .Select(s => unique.RandomItem())
                .ToArray();
        }
    }
}