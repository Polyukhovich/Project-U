using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using static System.Net.Mime.MediaTypeNames;

namespace ProjectU.Core.Services
{
    using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
    // Сервіс для витягування тексту з файлів .docx та .pdf
    public class FileTextExtractorService
    {
        // Витягує текст з файлу залежно від розширення
        public string ExtractText(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();

            return extension switch
            {
                ".docx" => ExtractFromDocx(filePath),
                ".pdf" => ExtractFromPdf(filePath),
                _ => string.Empty
            };
        }

        // Витягує текст з .docx файлу
        private string ExtractFromDocx(string filePath)
        {
            try
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return string.Empty;

                return string.Join(" ", body.Descendants<Text>()
                    .Select(t => t.Text));
            }
            catch
            {
                return string.Empty;
            }
        }

        // Витягує текст з .pdf файлу
        private string ExtractFromPdf(string filePath)
        {
            try
            {
                using var pdf = PdfDocument.Open(filePath);
                return string.Join(" ", pdf.GetPages()
                    .SelectMany(p => p.GetWords())
                    .Select(w => w.Text));
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}