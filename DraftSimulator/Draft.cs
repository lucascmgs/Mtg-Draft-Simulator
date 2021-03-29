using System;
using System.Collections.Generic;
using System.Linq;
using Scryfall.API;
using Scryfall.API.Models;

namespace DraftSimulator
{
    public class Draft
    {
        private bool _started = false;
        public bool Started
        {
            get { return _started; }
        }
        
        public long GroupId;

        private List<Player> _players;

        private List<List<List<Card>>> _rounds;

        private static ScryfallClient _scryfall;

        private CardList _cards;

        internal RarityDistribution CurrentRarityDistribution;

        public int CurrentRound;

        public int CurrentPick;

        public int NumberOfPlayers
        {
            get { return _players.Count; }
        }

        private string _set;

        public static ScryfallClient ScryfallInstance
        {
            get { return _scryfall ??= new ScryfallClient(); }
        }

        public Draft(long groupId, String set = "war")
        {
            GroupId = groupId;
            _set = set;
            _cards = ScryfallInstance.Cards.Search($"set:{_set}");

            if (!_cards.Data.Any())
            {
                throw new InexistentSetException();
            }
            
            _players = new List<Player>();
        }

        public void StartDraft(int packsPerPlayer = 3,
            RarityDistribution distribution = null)
        {
            CurrentRarityDistribution = distribution ?? new RarityDistribution();

            _rounds = new List<List<List<Card>>>();
            

            for (int j = 0; j < packsPerPlayer; j++)
            {
                var currentRound = new List<List<Card>>();
                for (int i = 0; i < NumberOfPlayers; i++)
                {
                    var currentPack = PackMaker.MakePack(_cards, CurrentRarityDistribution);
                    currentRound.Add(currentPack.ToList());
                }

                _rounds.Add(currentRound);
            }

            _started = true;
        }

        public void AddPlayer(String playerName, long id)
        {
            var playerExists = _players.Exists(p => p.Name == playerName);
            if (playerExists)
            {
                throw new PlayerExistsException();
            }


            var newPlayer = new Player(playerName, id, _players.Count + 1, this);
            _players.Add(newPlayer);
        }

        public string ListPack(long playerId)
        {
            var player = _players.Find(p => p.Id == playerId);
            if (player == null)
            {
                return "";
            }

            int currentPackNumber = (player.CurrentPick + player.StartingIndex) % NumberOfPlayers;
            var currentPack = _rounds.ElementAt(player.CurrentRound).ElementAt(currentPackNumber);
            if (player.CurrentPick + currentPack.Count > CurrentRarityDistribution.Count)
            {
                int difference = player.CurrentPick + currentPack.Count - CurrentRarityDistribution.Count;
                string s = difference > 1 ? "s" : "";
                return $"You cannot see the pack yet. You are {difference} pick{s} ahead.";
            }

            String listedPack = "";
            int i = 0;
            foreach (var card in currentPack)
            {
                listedPack +=
                    $"{i} [{card.Name}]({card.ScryfallUri}) {card.ManaCost}\n{card.TypeLine}\n{card.OracleText}";

                if (card.TypeLine.Contains("Creature"))
                {
                    listedPack += $"\n{card.Power}/{card.Toughness}";
                }
                else if (card.TypeLine.Contains("Planeswalker"))
                {
                    listedPack += $"\n{card.Loyalty}";
                }

                listedPack += "\n\n;;";
                i++;
            }

            return listedPack;
        }

        public string ListPool(long playerId)
        {
            var player = _players.Find(p => p.Id == playerId);
            if (player == null)
            {
                return "";
            }

            string listedPool = "";
            listedPool += $"Round: {player.CurrentRound + 1}\n";
            listedPool += $"Pick: {player.CurrentPick + 1}\n\n";
            listedPool += $"Your card pool is:\n";


            foreach (var cardEntry in player.PickedCards)
            {
                listedPool += $"{cardEntry.Value} {cardEntry.Key.Name}\n";
            }

            return listedPool;
        }

        public string ListPlayers()
        {
            string playerList = "";
            foreach (var player in _players)
            {
                playerList += $"{player.Name}\n";
            }

            return playerList;
        }

        public string Pick(long playerId, int cardIndexInPack)
        {
            var player = _players.Find(p => p.Id == playerId);
            if (player == null)
            {
                throw new Exception("The player is not registered!");
            }

            int currentPackNumber = (player.CurrentPick + player.StartingIndex) % NumberOfPlayers;
            var currentPack = _rounds.ElementAt(player.CurrentRound).ElementAt(currentPackNumber);

            if (player.CurrentPick + currentPack.Count > CurrentRarityDistribution.Count)
            {
                int difference = player.CurrentPick + currentPack.Count - CurrentRarityDistribution.Count;
                return $"You cannot pick yet. You are {difference} picks ahead";
            }

            var card = currentPack.ElementAt(cardIndexInPack);

            if (player.PickedCards.ContainsKey(card))
            {
                player.PickedCards[card] += 1;
            }
            else
            {
                player.PickedCards.Add(card, 1);
            }


            player.CurrentPick += 1;
            currentPack.Remove(card);
            return "Pick confirmed!";
        }
    }

    public class InexistentSetException : Exception
    {
    }

    public class PlayerExistsException : Exception
    {
    }

    public class Player
    {
        internal String Name;
        internal long Id;
        internal int StartingIndex;
        private readonly Draft _draft;
        internal Dictionary<Card, int> PickedCards;

        private int _currentPick;

        internal int CurrentPick
        {
            get => _currentPick;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                if (value > _draft.CurrentRarityDistribution.Count)
                {
                    _currentPick = 0;
                    CurrentRound += 1;
                }
                else
                {
                    _currentPick = value;
                }
            }
        }

        internal int CurrentRound = 0;

        public Player(string name, long id, int startingIndex, Draft draft)
        {
            _draft = draft;
            StartingIndex = startingIndex;
            PickedCards = new Dictionary<Card, int>();
            Id = id;
            Name = name;
        }
    }
}