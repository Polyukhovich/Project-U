using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectU.Core.Services
{
    // Сервіс перевірки текстових запозичень на основі методу шинглів
    public class PlagiarismService
    {
        // Розмір шинглу (кількість слів)
        private const int ShingleSize = 5;

        // Нормалізація тексту — видалення пунктуації, lowercase
        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Видаляємо всі символи крім літер та пробілів
            var normalized = new string(text
                .ToLower()
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());

            // Видаляємо зайві пробіли
            return string.Join(" ", normalized.Split(
                new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // Генерація множини шинглів з тексту
        public HashSet<string> GetShingles(string text)
        {
            var shingles = new HashSet<string>();
            var normalized = NormalizeText(text);
            var words = normalized.Split(' ');

            if (words.Length < ShingleSize)
                return shingles;

            for (int i = 0; i <= words.Length - ShingleSize; i++)
            {
                // Беремо ShingleSize слів підряд і об'єднуємо в один шингл
                var shingle = string.Join(" ", words.Skip(i).Take(ShingleSize));
                shingles.Add(shingle);
            }

            return shingles;
        }

        // Обчислення коефіцієнта Жаккара — міра схожості двох текстів
        public double CalculateSimilarity(HashSet<string> shingles1, HashSet<string> shingles2)
        {
            if (shingles1.Count == 0 || shingles2.Count == 0)
                return 0.0;

            // |A ∩ B| — кількість спільних шинглів
            var intersection = shingles1.Intersect(shingles2).Count();

            // |A ∪ B| — кількість унікальних шинглів в обох множинах
            var union = shingles1.Union(shingles2).Count();

            // Жаккар = |A ∩ B| / |A ∪ B|
            return (double)intersection / union * 100.0;
        }

        // Порівняння двох текстів — повертає відсоток схожості
        public double CompareTexts(string text1, string text2)
        {
            var shingles1 = GetShingles(text1);
            var shingles2 = GetShingles(text2);
            return CalculateSimilarity(shingles1, shingles2);
        }
    }
}
