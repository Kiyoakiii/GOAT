namespace GoatDescent
{
    public static class SlopeRun
    {
        public static bool Finished { get; private set; }
        public static void Notify(string message) { }
        public static void ResetRun() => Finished = false;
    }
}