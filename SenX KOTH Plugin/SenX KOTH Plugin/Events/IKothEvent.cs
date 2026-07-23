namespace SenX_KOTH_Plugin.Events;

internal interface IKothEvent
{
    string Name { get; }
    bool ShouldRun { get; }
    bool IsRunning { get; }
    void Start();
    void Stop();
    void Update();
    void IntegrityCheck();
    void Save();
}