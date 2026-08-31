using LarvaX.Core.Entities;

namespace LarvaX.Core.Interfaces
{
    public interface IEducationService
    {
        Task<IEnumerable<Article>> GetArticlesAsync(string language);
        Task<Article?> GetArticleByIdAsync(int id);
        Task<IEnumerable<Quiz>> GetQuizzesAsync(string language);
        Task<Quiz?> GetQuizByIdAsync(int id);
        Task<int> SubmitQuizAsync(int quizId, Dictionary<int, int> answers);
    }
}
