using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace WpfApp1
{
    public static class CourseAvailabilityManager
    {
        private static System.Timers.Timer _dailyTimer;
        private static readonly string ConnectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";

        public static event Action CoursesAvailabilityUpdated;

        public static void Initialize()
        {
            // Первая проверка при запуске
            CheckAndUpdateCourseAvailability();

            // Настройка таймера
            SetupDailyTimer();
        }

        private static void SetupDailyTimer()
        {
            // Вычисляем время до следующей полночи
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);
            var timeUntilMidnight = nextMidnight - now;

            _dailyTimer = new System.Timers.Timer(timeUntilMidnight.TotalMilliseconds);
            _dailyTimer.Elapsed += DailyCheckCallback;
            _dailyTimer.AutoReset = false;
            _dailyTimer.Start();
        }

        private static void DailyCheckCallback(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Выполняем проверку
            CheckAndUpdateCourseAvailability();

            // Перезапускаем таймер на следующие сутки
            _dailyTimer.Interval = TimeSpan.FromDays(1).TotalMilliseconds;
            _dailyTimer.Start();
        }

        private static void CheckAndUpdateCourseAvailability()
        {
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                        UPDATE courses 
                        SET availability = 0 
                        WHERE available_until IS NOT NULL 
                        AND date(available_until) < date('now')", conn);
                    int updated = cmd.ExecuteNonQuery();

                    if (updated > 0)
                    {
                        NotifyWindowsAboutUpdate();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении доступности курсов: {ex.Message}");
            }
        }

        private static void NotifyWindowsAboutUpdate()
        {
            Application.Current.Dispatcher.Invoke(new Action(InvokeCoursesAvailabilityUpdated));
        }

        private static void InvokeCoursesAvailabilityUpdated()
        {
            if (CoursesAvailabilityUpdated != null)
            {
                CoursesAvailabilityUpdated();
            }
        }
    }
}