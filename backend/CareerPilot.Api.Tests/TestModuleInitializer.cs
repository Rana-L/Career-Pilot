using System.Runtime.CompilerServices;

namespace CareerPilot.Api.Tests;

internal static class TestModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }
}
