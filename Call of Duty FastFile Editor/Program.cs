using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Call_of_Duty_FastFile_Editor.Services;

namespace Call_of_Duty_FastFile_Editor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Create the service collection
            var services = new ServiceCollection();

            // Register your application services
            services.AddSingleton<IRawFileService, RawFileService>();

            // Register your forms so they get DependencyInjected
            services.AddSingleton<MainWindowForm>();

            // Build the provider
            var provider = services.BuildServiceProvider();

            // 5) Run the app by resolving MainWindowForm from the container
            Application.Run(provider.GetRequiredService<MainWindowForm>());
        }
    }
}