using System.Text;

namespace AthkarApp.Services;

public enum WordStatus
{
    Correct,
    Missing,
    Wrong
}

public class WordAssessment
{
    public string Word { get; set; } = string.Empty;
    public WordStatus Status { get; set; }
}

public interface IHifzAssessmentService
{
    List<WordAssessment> AssessRecitation(string originalText, string recognizedText);
}

public class HifzAssessmentService : IHifzAssessmentService
{
    private readonly IQuranNormalizationService _normalizationService;

    public HifzAssessmentService(IQuranNormalizationService normalizationService)
    {
        _normalizationService = normalizationService;
    }

    public List<WordAssessment> AssessRecitation(string originalText, string recognizedText)
    {
        var result = new List<WordAssessment>();
        
        string normOriginal = _normalizationService.Normalize(originalText);
        string normRecognized = _normalizationService.Normalize(recognizedText);

        string[] originalWords = originalText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string[] normOriginalWords = normOriginal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string[] normRecognizedWords = normRecognized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Simple word-by-word comparison (for a starting point)
        // Note: For a more advanced system, a Levenshtein or Smith-Waterman alignment would be better
        int recIndex = 0;
        for (int i = 0; i < normOriginalWords.Length; i++)
        {
            var assessment = new WordAssessment { Word = originalWords[i] };
            
            if (recIndex < normRecognizedWords.Length && normOriginalWords[i] == normRecognizedWords[recIndex])
            {
                assessment.Status = WordStatus.Correct;
                recIndex++;
            }
            else if (recIndex < normRecognizedWords.Length && normRecognizedWords.Any(w => w == normOriginalWords[i]))
            {
                // Word found somewhere else (maybe user repeated or skipped a word)
                // For now, mark as wrong if not in the right sequence
                assessment.Status = WordStatus.Wrong;
            }
            else
            {
                assessment.Status = WordStatus.Missing;
            }
            
            result.Add(assessment);
        }

        return result;
    }
}
