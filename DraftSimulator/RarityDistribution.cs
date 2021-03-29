namespace DraftSimulator
{
    public class RarityDistribution
    {
        public int NumberOfCommons { get; set; }

        public int NumberOfUncommons { get; set; }
        public int NumberOfRares { get; set; }
        public double ChanceOfMythic { get; set; }
        public double ChanceOfFoil { get; set; }


        private int _count;

        public int Count
        {
            get
            {
                if (_count == 0)
                {
                    _count = NumberOfCommons + NumberOfUncommons + NumberOfRares;
                }

                return _count;
            }
        }

        public RarityDistribution()
        {
            NumberOfCommons = 10;
            NumberOfUncommons = 4;
            NumberOfRares = 1;
            ChanceOfMythic = 0.1351351351;
            ChanceOfFoil = 0.15;
        }
    }
}

