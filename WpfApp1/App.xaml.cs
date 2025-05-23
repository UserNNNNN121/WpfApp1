using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Главный класс приложения WPF.
    /// Наследуется от Application и управляет жизненным циклом приложения.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Метод, вызываемый при запуске приложения.
        /// Инициализирует менеджер доступности курсов.
        /// </summary>
        /// <param name="e">Аргументы запуска приложения</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CourseAvailabilityManager.Initialize();
        }
    }
}
