using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace MvcApp.Template
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(MvcAppTemplatePackage.PackageGuidString)]
    public sealed class MvcAppTemplatePackage : AsyncPackage
    {
        public const string PackageGuidString = "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";
    }
}
