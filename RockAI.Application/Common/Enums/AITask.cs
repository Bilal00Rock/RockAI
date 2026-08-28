using Ardalis.SmartEnum;

namespace RockAI.Application.Common.Enums;

public sealed class AITask : SmartEnum<AITask>
{
    public static readonly AITask Chat = new(nameof(Chat), 1);
    public static readonly AITask MasterDataExtraction = new(nameof(MasterDataExtraction), 2);
    public static readonly AITask CVExtraction = new(nameof(CVExtraction), 3);
    public static readonly AITask ImageAnalysis = new(nameof(ImageAnalysis), 4);
    public static readonly AITask General = new(nameof(General), 5);
    private AITask(string name, int value) : base(name, value)
    {
    }
}