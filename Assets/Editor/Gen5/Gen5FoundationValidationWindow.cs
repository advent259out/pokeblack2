using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PokeBlack2.Foundation.Editor
{
    public sealed class Gen5FoundationValidationWindow : EditorWindow
    {
        private const string WindowTitle = "Gen5 Validation";

        private string rootPath = Gen5ImportProfile.CanonicalExportRoot;
        private Vector2 scrollPosition;
        private Gen5FoundationValidationArtifactSnapshot snapshot;
        private string lastError = string.Empty;
        private string lastStatus = "No validation artifacts loaded yet.";

        [MenuItem("PokeBlack2/Gen5/Open Foundation Validation Window")]
        public static void OpenWindow()
        {
            Gen5FoundationValidationWindow window = GetWindow<Gen5FoundationValidationWindow>(WindowTitle);
            window.minSize = new Vector2(760f, 520f);
            window.Show();
        }

        public static void OpenWindowAndLoadArtifacts(string rootPath)
        {
            Gen5FoundationValidationWindow window = GetWindow<Gen5FoundationValidationWindow>(WindowTitle);
            window.minSize = new Vector2(760f, 520f);
            window.Show();
            window.LoadArtifactsFromRoot(rootPath);
            window.Focus();
        }

        private void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                rootPath = Gen5ImportProfile.CanonicalExportRoot;
            }

            string reportPath = Gen5FoundationValidationArtifactReader.ResolveReportPath(rootPath);
            if (File.Exists(reportPath))
            {
                TryLoadArtifacts(rootPath, "Loaded existing validation artifacts.");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Gen5 Foundation Validation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Validate the offline export seam and inspect the latest report without leaving the Unity editor.",
                MessageType.Info);

            DrawRootControls();
            EditorGUILayout.Space(8f);
            DrawActionButtons();
            EditorGUILayout.Space(8f);
            DrawStatus();
            EditorGUILayout.Space(8f);
            DrawArtifacts();
        }

        private void DrawRootControls()
        {
            EditorGUILayout.LabelField("Export Root", EditorStyles.boldLabel);
            rootPath = EditorGUILayout.TextField(rootPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Canonical Root"))
                {
                    rootPath = Gen5ImportProfile.CanonicalExportRoot;
                }

                if (GUILayout.Button("Browse..."))
                {
                    string defaultDirectory = Directory.Exists(rootPath)
                        ? Path.GetFullPath(rootPath)
                        : Directory.GetCurrentDirectory();
                    string selectedPath = EditorUtility.OpenFolderPanel("Select Gen5 export root", defaultDirectory, string.Empty);
                    if (!string.IsNullOrWhiteSpace(selectedPath))
                    {
                        rootPath = selectedPath;
                    }
                }
            }
        }

        private void DrawActionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Latest Artifacts"))
                {
                    LoadArtifactsFromRoot(rootPath);
                }

                if (GUILayout.Button("Validate + Write Artifacts"))
                {
                    RunValidation(rootPath);
                }

                using (new EditorGUI.DisabledScope(snapshot == null))
                {
                    if (GUILayout.Button("Reveal Report"))
                    {
                        EditorUtility.RevealInFinder(snapshot.ReportPath);
                    }

                    if (GUILayout.Button("Reveal Summary"))
                    {
                        EditorUtility.RevealInFinder(snapshot.SummaryPath);
                    }
                }
            }
        }

        private void DrawStatus()
        {
            MessageType messageType = string.IsNullOrWhiteSpace(lastError) ? MessageType.Info : MessageType.Error;
            string message = string.IsNullOrWhiteSpace(lastError) ? lastStatus : lastError;
            EditorGUILayout.HelpBox(message, messageType);
        }

        private void DrawArtifacts()
        {
            if (snapshot == null || snapshot.Report == null)
            {
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Artifact Paths", EditorStyles.boldLabel);
            DrawSelectableField("Root", snapshot.RootPath);
            DrawSelectableField("Report", snapshot.ReportPath);
            DrawSelectableField("Summary", snapshot.SummaryPath);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(snapshot.DisplaySummary, GUILayout.MinHeight(110f));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Report Overview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Game", snapshot.Report.Game);
            EditorGUILayout.LabelField("ROM", snapshot.Report.RomFilename);
            EditorGUILayout.LabelField("SHA1", snapshot.Report.RomSha1);
            EditorGUILayout.LabelField("ROM Size", snapshot.Report.RomSize.ToString());
            EditorGUILayout.LabelField("Sources", snapshot.Report.SourceCount.ToString());
            EditorGUILayout.LabelField(
                "Available Groups",
                $"{snapshot.Report.AvailableGroupCount}/{snapshot.Report.GroupSummaries.Count}");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Groups", EditorStyles.boldLabel);
            foreach (Gen5FoundationGroupValidationSummary groupSummary in snapshot.Report.GroupSummaries)
            {
                MessageType type = groupSummary.IsAvailable ? MessageType.Info : MessageType.Warning;
                string availability = groupSummary.IsAvailable ? "available" : "missing";
                EditorGUILayout.HelpBox(
                    $"{groupSummary.GroupName}: {availability} | sources={groupSummary.SourceCount} | containers={groupSummary.ContainerCount}",
                    type);
            }

            EditorGUILayout.EndScrollView();
        }

        private void LoadArtifactsFromRoot(string root)
        {
            TryLoadArtifacts(root, "Loaded validation artifacts.");
        }

        private void RunValidation(string root)
        {
            try
            {
                Gen5FoundationValidationArtifactSet artifacts = Gen5FoundationImportRunner.ValidateAndWriteArtifacts(root);
                snapshot = Gen5FoundationValidationArtifactReader.LoadFromRoot(artifacts.RootPath);
                lastStatus = $"Validation passed and artifacts were refreshed for '{snapshot.RootPath}'.";
                lastError = string.Empty;
            }
            catch (Exception exception)
            {
                snapshot = null;
                lastError = exception.Message;
            }
        }

        private void TryLoadArtifacts(string root, string successMessage)
        {
            try
            {
                snapshot = Gen5FoundationValidationArtifactReader.LoadFromRoot(root);
                lastStatus = $"{successMessage} Root: '{snapshot.RootPath}'.";
                lastError = string.Empty;
            }
            catch (Exception exception)
            {
                snapshot = null;
                lastError = exception.Message;
            }
        }

        private static void DrawSelectableField(string label, string value)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(
                value ?? string.Empty,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight * 2.2f));
        }
    }
}
