namespace TurkceRumenceCeviri.Services;

public interface IAIAssistantService
{
    Task<string> AnswerQuestionAsync(string question, string context, string language);
}
