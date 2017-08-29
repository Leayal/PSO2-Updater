namespace Leayal.PSO2.Updater
{
    internal class InstallationMeta
    {
        public string GamePath { get; }
        public string Step { get; }
        public InstallationMeta(string step) : this(step, string.Empty) { }
        public InstallationMeta(string step, string path)
        {
            this.Step = step;
            this.GamePath = path;
        }
    }
}
