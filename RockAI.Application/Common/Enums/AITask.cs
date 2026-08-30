using Ardalis.SmartEnum;

namespace RockAI.Application.Common.Enums;

public sealed class AITask : SmartEnum<AITask>
{
    public static readonly AITask Chat = new(nameof(Chat), 1);
    public static readonly AITask PDFAnalysis = new(nameof(PDFAnalysis), 2);
    public static readonly AITask CodeAnalysis = new(nameof(CodeAnalysis), 3);
    public static readonly AITask ImageAnalysis = new(nameof(ImageAnalysis), 4);
    public static readonly AITask Intract = new(nameof(Intract), 5);
    public static readonly AITask General = new(nameof(General), 6);
    private AITask(string name, int value) : base(name, value)
    {
    }
}