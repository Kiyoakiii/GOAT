namespace GoatDescent.Harness
{
    public interface IAgentGame
    {
        bool Ready { get; }
        string MapSignature { get; }
        void Reset(string scenarioId, int seed);
        void Execute(GameCommand command);
        GameSnapshot Snapshot(int tick);
    }
}
