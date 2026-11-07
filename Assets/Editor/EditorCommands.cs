using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Rimbara.Editor
{
    // A local, opt-in command inbox lets the build scripts use an already-open
    // Editor. It accepts only the named project operations below, not arbitrary code.
    [InitializeOnLoad]
    public static class EditorCommands
    {
        [Serializable]
        private sealed class Request
        {
            public string Command;
            public string PackagePath;
            public string RequestId;
        }

        [Serializable]
        private sealed class Response
        {
            public string RequestId;
            public string Command;
            public bool Success;
            public string Message;
        }

        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string Inbox => Path.Combine(Root, "Artifacts", "editor-command.v2.json");

        static EditorCommands()
        {
            Directory.CreateDirectory(Path.Combine(Root, "Artifacts"));
            File.WriteAllText(Path.Combine(Root, "Artifacts", "editor-bridge-ready.txt"), "3 / " + DateTime.UtcNow.ToString("O"));
            if (!Application.isBatchMode) EditorApplication.update += ProcessInbox;
        }

        private static void ProcessInbox()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying
                || !File.Exists(Inbox)) return;
            string running = Inbox + ".executing";
            if (File.Exists(running)) return;
            File.Move(Inbox, running);
            Request request = null;
            try
            {
                request = JsonUtility.FromJson<Request>(File.ReadAllText(running));
                switch (request.Command)
                {
                    case "assets": AcquireAssets.OpenStarterAssets(); break;
                    case "download-starter": AcquireAssets.DownloadStarterAssets(); break;
                    case "inspect": InspectPackageManager(); break;
                    case "import":
                        if (string.IsNullOrEmpty(request.PackagePath) || !File.Exists(request.PackagePath)
                            || !request.PackagePath.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("A local .unitypackage is required.");
                        AcquireAssets.ImportStarterPackage(request.PackagePath);
                        break;
                    case "setup": InvokeProjectOperation("SetupScene"); break;
                    case "build": InvokeProjectOperation("BuildWindows"); break;
                    case "verify": InvokeProjectOperation("VerifyProject"); break;
                    case "quit":
                        AssetDatabase.SaveAssets();
                        // Acknowledge first; exiting here would leave the
                        // PowerShell caller waiting for a response forever.
                        EditorApplication.delayCall += () => EditorApplication.Exit(0);
                        break;
                    default: throw new InvalidOperationException("Unknown RIMBARA Editor command: " + request.Command);
                }
                File.WriteAllText(Path.Combine(Root, "Artifacts", "editor-command-result.txt"), request.Command + ": OK");
                Respond(request, true, request.Command + ": OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                File.WriteAllText(Path.Combine(Root, "Artifacts", "editor-command-result.txt"), exception.ToString());
                Respond(request, false, exception.ToString());
            }
            finally { File.Delete(running); }
        }

        private static void Respond(Request request, bool success, string message)
        {
            string path = Path.Combine(Root, "Artifacts", "editor-command-result.json");
            string json = JsonUtility.ToJson(new Response
            {
                RequestId = request?.RequestId, Command = request?.Command, Success = success, Message = message
            }, true);
            // A unique response is never replaced while its caller reads it.
            // Keep the shared latest-result file only as a human-readable alias.
            if (Guid.TryParse(request?.RequestId, out Guid id))
            {
                string response = Path.Combine(Root, "Artifacts", "editor-response-" + id.ToString("N") + ".json");
                File.WriteAllText(response + ".new", json);
                File.Move(response + ".new", response);
            }
            File.WriteAllText(path + ".new", json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(path + ".new", path);
        }

        private static void InvokeProjectOperation(string method)
        {
            Type builder = typeof(EditorCommands).Assembly.GetType("Rimbara.Editor.ProjectBuilder");
            MethodInfo operation = builder?.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            if (operation == null) throw new InvalidOperationException("Project operation is not available yet: " + method);
            operation.Invoke(null, null);
        }

        private static void InspectPackageManager()
        {
            var lines = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.Namespace != null && t.Namespace.StartsWith("UnityEditor.PackageManager.UI")
                    && (t.Name == "Window" || t.Name.Contains("PackageManagerWindow") || t.Name.Contains("AssetStoreDownload") || t.Name == "AssetStoreUtils"))
                .Select(t => t.AssemblyQualifiedName + "\n" + string.Join("\n", t.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.Name.Contains("Open") || m.Name.Contains("Download") || m.Name.Contains("Import"))
                    .Select(m => "  " + m)));
            File.WriteAllText(Path.Combine(Root, "Artifacts", "package-manager-api.txt"), string.Join("\n\n", lines));
        }
    }
}
