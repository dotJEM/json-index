using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Test.Data
{
    public class AnimalDataDecorator : TestDataDecorator
    {
        struct Animal { public string name, latinName, introEra, area, taxonGroup; }

        private static readonly Animal[] animals =
        {
            new Animal { name="Dog",                        latinName="Canis lupus familiaris", introEra="13000 BCE", area="Europe", taxonGroup="Carnivora" },
            new Animal { name="Sheep",                      latinName="Ovis aries", introEra="9000 BCE to 8500 BCE", area="Anatolia,Zagros mountains", taxonGroup="Bovidae" },
            new Animal { name="Domestic pig",               latinName="Sus scrofa domesticus", introEra="9000 BC", area="Near East,China", taxonGroup="Artiodactyla except Bovidae" },
            new Animal { name="Domestic goat",              latinName="Capra aegagrus hircus", introEra="10000 BC", area="Near East", taxonGroup="Bovidae" },
            new Animal { name="Cattle",                     latinName="Bos primigenius taurus", introEra="8000 BC", area="India, Middle East, North Africa", taxonGroup="Bovidae" },
            new Animal { name="Zebu",                       latinName="Bos primigenius indicus", introEra="8000 BC", area="India", taxonGroup="Bovidae" },
            new Animal { name="Cat",                        latinName="Felis silvestris catus", introEra="8000 BC to 7500 BC", area="Near East", taxonGroup="Carnivora" },
            new Animal { name="Chicken",                    latinName="Gallus gallus domesticus", introEra="6000 BC", area="India, Southeast Asia", taxonGroup="Galliformes" },
            new Animal { name="Guinea pig",                 latinName="Cavia porcellus", introEra="5000 BC", area="Peru", taxonGroup="Rodentia" },
            new Animal { name="Donkey",                     latinName="Equus africanus asinus", introEra="5000 BC", area="Egypt", taxonGroup="Other mammals" },
            new Animal { name="Domestic duck",              latinName="Anas platyrhynchos domesticus", introEra="4000 BC", area="China", taxonGroup="Anseriformes" },
            new Animal { name="Horse",                      latinName="Equus ferus caballus", introEra="3500 BC", area="Kazakhstan", taxonGroup="Other mammals" },
            new Animal { name="Domestic dromedary camel",   latinName="Camelus dromedarius", introEra="4000 BC", area="Arabia", taxonGroup="Artiodactyla except Bovidae" },
            new Animal { name="Domestic silkmoth",          latinName="Bombyx mori", introEra="3000 BC", area="China", taxonGroup="Other insects" },
            new Animal { name="Domestic pigeon",            latinName="Columba livia domestica", introEra="3000 BC", area="Mediterranean Basin", taxonGroup="Columbiformes" },
            new Animal { name="Domestic goose",             latinName="Anser anser domesticus", introEra="3000 BC", area="Egypt, China", taxonGroup="Anseriformes" },
            new Animal { name="Yak",                        latinName="Bos grunniens", introEra="2500 BC", area="Tibet, Nepal", taxonGroup="Bovidae" },
            new Animal { name="Domestic Bactrian camel",    latinName="Camelus bactrianus", introEra="2500 BC", area="Central Asia(Afghanistan)", taxonGroup="Artiodactyla except Bovidae" },
            new Animal { name="Llama",                      latinName="Lama glama", introEra="2400 BC", area="Bolivia", taxonGroup="Artiodactyla except Bovidae" },
            new Animal { name="Alpaca",                     latinName="Vicugna pacos", introEra="2400 BC", area="Bolivia", taxonGroup="Artiodactyla except Bovidae" },
            new Animal { name="Domestic guineafowl",        latinName="Numida meleagris", introEra="2400 BC", area="Africa", taxonGroup="Galliformes" },
            new Animal { name="Ferret",                     latinName="Mustela putorius furo", introEra="1500 BC", area="Europe", taxonGroup="Carnivora" },
            new Animal { name="Fancy mouse",                latinName="Mus musculus", introEra="1800s AD", area="China", taxonGroup="Rodentia" },
            new Animal { name="Ringneck dove",              latinName="Streptopelia risoria", introEra="500 BC", area="North Africa", taxonGroup="Columbiformes" },
            new Animal { name="Bali cattle",                latinName="Bos javanicus domestica", introEra="unknown", area="Southeast Asia, Java", taxonGroup="Bovidae" },
            new Animal { name="Gayal",                      latinName="Bos frontalis", introEra="unknown", area="Southeast Asia", taxonGroup="Bovidae" },
            new Animal { name="Domestic turkey",            latinName="Meleagris gallopavo", introEra="180 AD", area="Mexico, United States", taxonGroup="Galliformes" },
            new Animal { name="Goldfish",                   latinName="Carassius auratus auratus", introEra="300 AD to 400 AD", area="China", taxonGroup="Cyprinidae" },
            new Animal { name="Domestic rabbit",            latinName="Oryctolagus cuniculus", introEra="600 AD", area="Europe", taxonGroup="Other mammals" },
            new Animal { name="Domestic canary",            latinName="Serinus canaria domestica", introEra="15th century AD", area="Canary Islands,Europe", taxonGroup="Passeriformes" },
            new Animal { name="Siamese fighting fish",      latinName="Betta splendens", introEra="19th century AD", area="Thailand", taxonGroup="Other fish" },
            new Animal { name="Koi",                        latinName="Cyprinus carpio haematopterus", introEra="1820s AD", area="Japan", taxonGroup="Cyprinidae" },
            new Animal { name="Domestic silver fox",        latinName="Vulpes vulpes", introEra="1950s AD", area="Soviet Union,Russia", taxonGroup="Carnivora" },
            new Animal { name="Domestic hedgehog",          latinName="Atelerix albiventris", introEra="1980s AD", area="Central Africa, Eastern Africa", taxonGroup="Other mammals" },
            new Animal { name="Society finch",              latinName="Lonchura striata domestica", introEra="unknown", area="Japan", taxonGroup="Passeriformes" }
        };

        public override JObject Decorate(JObject json, Random rnd)
        {
            json["type"] = "Animal";

            Animal select = Random(animals, rnd);
            json["name"] = select.name;
            json["latinName"] = select.latinName;
            json["introEra"] = select.introEra;
            json["region"] = JArray.FromObject(select.area.Split(',').Select(str => str.Trim()));
            json["taxonGroup"] = select.taxonGroup;
            return json;
        }
    }
}