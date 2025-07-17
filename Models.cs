
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CanvasQuizConverter.Models
{
    public class Quiz
    {
        [JsonPropertyName("quizTitle")]
        public string QuizTitle { get; set; }

        [JsonPropertyName("quizId")]
        public string QuizId { get; set; }

        [JsonPropertyName("multipleChoiceQuestions")]
        public List<MultipleChoiceQuestion> MultipleChoiceQuestions { get; set; } = new();

        [JsonPropertyName("freeResponseQuestions")]
        public List<FreeResponseQuestion> FreeResponseQuestions { get; set; } = new();
    }

    public class MultipleChoiceQuestion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("points")]
        public double Points { get; set; }

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; }

        [JsonPropertyName("answers")]
        public List<Answer> Answers { get; set; } = new();
    }

    public class Answer
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N");

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; }

        [JsonPropertyName("feedback")]
        public string? Feedback { get; set; }
    }

    public class FreeResponseQuestion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("points")]
        public double Points { get; set; }

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; }

        [JsonPropertyName("modelAnswer")]
        public string ModelAnswer { get; set; }
    }
}
