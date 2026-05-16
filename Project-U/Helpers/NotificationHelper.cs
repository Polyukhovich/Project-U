using Microsoft.Extensions.Localization;
using ProjectU.Core.Models;
using ProjectU.Data;
using System.Globalization;

namespace Project_U.Helpers
{
    public class NotificationHelper
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizerFactory _localizerFactory;

        public NotificationHelper(ApplicationDbContext context, IStringLocalizerFactory localizerFactory)
        {
            _context = context;
            _localizerFactory = localizerFactory;
        }

        public async Task<string> GetLocalizedMessage(string userId, string key, params object[] args)
        {
            var user = await _context.Users.FindAsync(userId);
            var language = user?.PreferredLanguage ?? "uk-UA";

            Console.WriteLine($"DEBUG: userId={userId}, language={language}, key={key}");

            var culture = new CultureInfo(language);
            var previousCulture = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentUICulture = culture;

                var localizer = _localizerFactory.Create(
                    "Notifications",
                    typeof(Program).Assembly.GetName().Name!);

                var message = localizer[key];

                Console.WriteLine($"DEBUG: message={message}");

                return string.Format(message, args);
            }
            finally
            {
                CultureInfo.CurrentUICulture = previousCulture;
            }
        }
    }
}