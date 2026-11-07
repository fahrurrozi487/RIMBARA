using System;
using System.Reflection;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rimbara.Editor
{
    public static class AcquireAssets
    {
        [MenuItem("RIMBARA/Open official Starter Assets in Package Manager")]
        public static void OpenStarterAssets()
        {
            // The Asset Store downloader is authenticated inside the Editor,
            // rather than sharing browser cookies or credentials with scripts.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type packageWindow = assembly.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow")
                    ?? assembly.GetType("UnityEditor.PackageManager.UI.Internal.PackageManagerWindow");
                MethodInfo openUrl = packageWindow?.GetMethod("OpenURL", BindingFlags.Public | BindingFlags.Static);
                if (openUrl != null)
                {
                    openUrl.Invoke(null, new object[] { "com.unity3d.kharma:content/196526" });
                    return;
                }
                Type window = assembly.GetType("UnityEditor.PackageManager.UI.Window");
                if (window == null) continue;
                MethodInfo open = window.GetMethod("Open", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string) }, null);
                if (open != null) { open.Invoke(null, new object[] { "196526" }); return; }
            }
            EditorApplication.ExecuteMenuItem("Window/Package Management/Package Manager");
            Debug.Log("In Package Manager, select My Assets, then First Person + Third Person | Character Controllers, then Download.");
        }

        private static string AcquisitionReport => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Artifacts", "starter-assets-acquisition.txt"));

        [MenuItem("RIMBARA/Download claimed official Starter Assets")]
        public static void DownloadStarterAssets()
        {
            OpenStarterAssets();
            object manager = ResolveService("AssetStoreDownloadManager");
            EventInfo finalized = manager.GetType().GetEvent("onDownloadFinalized");
            MethodInfo callback = typeof(AcquireAssets).GetMethod(nameof(OnDownloadFinalized), BindingFlags.NonPublic | BindingFlags.Static);
            finalized.AddEventHandler(manager, Delegate.CreateDelegate(finalized.EventHandlerType, callback));
            File.WriteAllText(AcquisitionReport, "Requested: official Asset Store downloader, product 196526.\nIf authentication is requested, sign in inside Unity Editor.");
            MethodInfo download = manager.GetType().GetMethod("Download", BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(System.Collections.Generic.IEnumerable<long>) }, null);
            download.Invoke(manager, new object[] { new long[] { 196526 } });
        }

        private static object ResolveService(string shortName)
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                a.GetType("UnityEditor.PackageManager.UI.Internal.ServicesContainer") != null);
            if (assembly == null) throw new InvalidOperationException("Unity Package Manager services are not loaded.");
            Type containerType = assembly.GetType("UnityEditor.PackageManager.UI.Internal.ServicesContainer");
            object container = containerType.GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
            Type serviceType = assembly.GetType("UnityEditor.PackageManager.UI.Internal." + shortName);
            MethodInfo resolve = containerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .First(m => m.Name == "Resolve" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            return resolve.MakeGenericMethod(serviceType).Invoke(container, null);
        }

        private static void OnDownloadFinalized(object operation)
        {
            Type operationType = operation.GetType();
            long id = Convert.ToInt64(operationType.GetProperty("productId").GetValue(operation));
            if (id != 196526) return;
            object cache = ResolveService("AssetStoreCache");
            object local = cache.GetType().GetMethod("GetLocalInfo").Invoke(cache, new object[] { id });
            string path = local?.GetType().GetProperty("packagePath")?.GetValue(local) as string
                ?? local?.GetType().GetField("packagePath")?.GetValue(local) as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                File.WriteAllText(AcquisitionReport, "Failed: no downloaded package was registered. Check Package Manager authentication and download errors.");
                return;
            }
            ImportStarterPackage(path);
        }

        public static void ImportStarterPackage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new FileNotFoundException("The official Starter Assets package has not been downloaded.", path);
            File.WriteAllText(AcquisitionReport, "Downloaded: " + path + "\nImporting the official package.");
            AssetDatabase.importPackageCompleted += OnImportCompleted;
            AssetDatabase.importPackageFailed += OnImportFailed;
            AssetDatabase.ImportPackage(path, false);
        }

        private static void OnImportCompleted(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            File.WriteAllText(AcquisitionReport, "Imported: " + packageName);
        }

        private static void OnImportFailed(string packageName, string error)
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            File.WriteAllText(AcquisitionReport, "Failed: " + packageName + " / " + error);
        }
    }
}
