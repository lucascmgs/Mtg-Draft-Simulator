using System;
using System.Collections.Generic;
using System.Linq;
using Scryfall.API.Models;

namespace DraftSimulator
{
    public static class PackMaker
    {
        private static Random _random;

        public static IList<Card> MakePack(CardList cardSet, RarityDistribution rarityDistribution = null)
        {
            _random = new Random(DateTime.Now.Millisecond);
            
            rarityDistribution ??= new RarityDistribution();
            var commons = cardSet.Data.Where(c => c.Rarity == Rarity.Common);
            var uncommons = cardSet.Data.Where(c => c.Rarity == Rarity.Uncommon);
            var rares = cardSet.Data.Where(c => c.Rarity == Rarity.Rare);
            var mythics = cardSet.Data.Where(c => c.Rarity == Rarity.Mythic);

            var gotCards = new List<Card>();

            var commonsList = commons.ToList();
            var commonsCount = commonsList.Count;
            if (_random.Next(1, 100) / 100.0 < rarityDistribution.ChanceOfFoil)
            {
                for (var i = 0; i < rarityDistribution.NumberOfCommons-1; i++)
                {
                    gotCards.Add(commonsList.ElementAt(_random.Next(1, commonsCount)));
                }
                gotCards.Add(cardSet.Data.ElementAt(_random.Next(1, cardSet.Data.Count())));
            }
            else
            {
                for (var i = 0; i < rarityDistribution.NumberOfCommons; i++)
                {
                    gotCards.Add(commonsList.ElementAt(_random.Next(1, commonsCount)));
                }
            }

            var uncommonsList = uncommons.ToList();
            var uncommonsCount = uncommonsList.Count;
            
            
            for (int i = 0; i < rarityDistribution.NumberOfUncommons; i++)
            {
                gotCards.Add(uncommonsList.ElementAt(_random.Next(1, uncommonsCount)));
            }

            var mythicsList = mythics.ToList();
            var mythicsCount = mythicsList.Count;
            
            var raresList = rares.ToList();
            var raresCount = mythicsList.Count;
            
            for (int i = 0; i < rarityDistribution.NumberOfRares; i++)
            {
                if (_random.Next(1, 100) / 100.0 < rarityDistribution.ChanceOfMythic)
                {
                    gotCards.Add(mythicsList.ElementAt(_random.Next(1, mythicsCount)));
                }
                else
                {
                    gotCards.Add(raresList.ElementAt(_random.Next(1, raresCount)));
                }
            }
            
            return gotCards;
        }
    }
}