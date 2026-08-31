using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Infrastructure.Services
{
    public class EducationService : IEducationService
    {
        private readonly ApplicationDbContext _db;

        public EducationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Article>> GetArticlesAsync(string language)
        {
            return await _db.Articles
                .Where(a => a.Language == language)
                .OrderByDescending(a => a.PublishedDate)
                .ToListAsync();
        }

        public async Task<Article?> GetArticleByIdAsync(int id)
        {
            return await _db.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Quiz>> GetQuizzesAsync(string language)
        {
            return await _db.Quizzes
                .Where(q => q.Language == language)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .ToListAsync();
        }

        public async Task<Quiz?> GetQuizByIdAsync(int id)
        {
            return await _db.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<int> SubmitQuizAsync(int quizId, Dictionary<int, int> answers)
        {
            var quiz = await GetQuizByIdAsync(quizId);
            if (quiz == null) return 0;

            int score = 0;
            foreach (var question in quiz.Questions)
            {
                if (answers.TryGetValue(question.Id, out var selectedOptionId))
                {
                    var correct = question.Options.FirstOrDefault(o => o.IsCorrect);
                    if (correct != null && correct.Id == selectedOptionId)
                        score++;
                }
            }
            return score;
        }
    }
}
