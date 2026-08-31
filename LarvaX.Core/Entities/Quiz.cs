namespace LarvaX.Core.Entities
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }

    public class QuizQuestion
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz? Quiz { get; set; }
        public string Text { get; set; } = string.Empty;
        public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
    }

    public class QuizOption
    {
        public int Id { get; set; }
        public int QuizQuestionId { get; set; }
        public QuizQuestion? Question { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
