namespace RockAI.Application.Common.Interfaces;

public interface IAIEndpoints
{
    Uri BaseAddress { get; }
    string Tags { get; }
    string Generate { get; }
    string Chat { get; }
    bool UseProxy { get; }
}
