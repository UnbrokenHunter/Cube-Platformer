using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Porkchop.SceneSorter
{
    public class SceneSorterEditor : EditorWindow
    {
        private static List<SceneSorterElement> allSceneLocations;

        private enum FocusState
        {
            NoFocus,
            PartialFocus, // any movement to a different scene will change the focus.
            FixedFocus, // the focus is fixed on this scene name no matter what position its in.
        };

        private static FocusState _focusState = FocusState.NoFocus;
        private static string _focusedSceneName = null;
        private static int _windowWidth = 0;
        private static int _textLineLeft = 0;
        private static int _textLineRight = 0;

        [MenuItem("Tools/Scene Sorter/Open Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneSorterEditor>(false, "Scene Sorter", true);
            window.Show();
            window.Startup();
        }

        private void OnEnable()
        {
            Startup();
        }

        void Startup()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            if (SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Logo) == null)
            {
                SceneSorterGlobals.LoadMyResources();
            }

            if (Preferences.WindowIcon)
                titleContent.image = SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Love);
            else
                titleContent.image = null;
            RefreshSceneList();
        }

        void OnLostFocus()
        {
            NoFocus();
        }

        void NoFocus()
        {
            _focusState = FocusState.NoFocus;
            _focusedSceneName = null;
            ForceRepaint();
        }

        void PartialFocus(string sceneName)
        {
            _focusState = FocusState.PartialFocus;
            _focusedSceneName = sceneName;
            
            // Highlight this scene in the project window.
            Selection.activeInstanceID = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/" + sceneName + ".unity").GetInstanceID();
            
            ForceRepaint();
        }

        void FixedFocus(string sceneName)
        {
            _focusState = FocusState.FixedFocus;
            _focusedSceneName = sceneName;
            ForceRepaint();
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            SceneSorterSerializer.Save(allSceneLocations);
        }

        #region Loading and refreshing

        private void RefreshSceneList()
        {
            allSceneLocations = SceneSorterSerializer.Load();

            int trimLeftLen = Application.dataPath.Length + 1;
            string[] scenes = Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories);
            foreach (string sceneName in scenes)
            {
                // Remove the data path.
                // Convert backslashes to forward slashes.
                int len = sceneName.Length;
                string name = sceneName.Substring(trimLeftLen, len - trimLeftLen - 6).Replace('\\', '/');

                // If the scene isn't already in our list we should add it.
                if (ContainsSceneName(name) == false)
                {
                    SceneSorterElement element = new SceneSorterElement
                    {
                        favorite = false,
                        name = name
                    };
                    allSceneLocations.Add(element);
                }
            }
        }

        private bool ContainsSceneName(string testName)
        {
            foreach (var element in allSceneLocations)
            {
                if (element.name == testName)
                    return true;
            }

            return false;
        }

        #endregion

        private static bool _forceRepaint = false;

        #region GUI

        void OnGUI()
        {
            _forceRepaint = false;
            
            // Trim any scene names that are too long to fit.
            if( _windowWidth != (int)position.width || Preferences.TriggerTrim )
            {
                Preferences.TriggerTrim = false;
                _windowWidth = (int)position.width;
                ForceTrimSceneNames();
                ForceRepaint();
            }

            GUIDrawLogo();
            GUIDrawAndTestButtons();

            if (_forceRepaint)
            {
                _forceRepaint = false;
                Repaint();
            }
        }

        void ForceTrimSceneNames()
        {
            float textLineLeft = SceneSorterGlobals.LeftMargin +
                                     ((SceneSorterGlobals.ButtonWidth + SceneSorterGlobals.HorizPad) * 3.0f);
            float textLineRight = position.width - SceneSorterGlobals.RightMargin -
                                  ((SceneSorterGlobals.NoBorderButtonWidth + SceneSorterGlobals.HorizPad) * 2.0f) - 10.0f;
            int nameWidth = (int)(textLineRight - textLineLeft);

            _textLineLeft = (int)textLineLeft;
            _textLineRight = (int)textLineRight;
            
            if (Preferences.TrimOption == Preferences.NameTrimOption.ShowEnd)
            {
                foreach (var t in allSceneLocations)
                    t.trimmedName = TrimStringShowEnd(t.name, (int)nameWidth);
            }
            else if (Preferences.TrimOption == Preferences.NameTrimOption.TrimCenter)
            {
                foreach (var t in allSceneLocations)
                    t.trimmedName = TrimStringByCenter(t.name, (int)nameWidth);
            }
            else if( Preferences.TrimOption == Preferences.NameTrimOption.NameOnly)
            {
                foreach (var t in allSceneLocations)
                    t.trimmedName = TrimStringToNameOnly(t.name);
            }
            else if( Preferences.TrimOption == Preferences.NameTrimOption.FullPath)
            {
                foreach (var t in allSceneLocations)
                    t.trimmedName = TrimStringShowStart(t.name,(int)nameWidth);
            }
        }

        void GUIDrawLogo()
        {
            var logo = SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Logo);
            var logoStretch = SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.LogoStretch);
            var logoCap = SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.LogoCap);
            if (logo == null || logoStretch == null || logoCap == null)
                return;

            Rect logoRect = new Rect(0, 0, logo.width, logo.height);
            GUI.DrawTexture(logoRect, logo);

            Rect logoRectStretch = new Rect(logo.width, 0, position.width - logo.width - logoCap.width,
                logoStretch.height);
            GUI.DrawTexture(logoRectStretch, logoStretch);

            Rect logoRectCap = new Rect(position.width - logoCap.width, 0, logoCap.width, logoCap.height);
            GUI.DrawTexture(logoRectCap, logoCap);
        }

        Vector2 _scrollPos = Vector2.zero;
        float maxHeight = -1.0f;

        string PrepForPathComparison(string path1)
        {
            return path1.Replace("\\", "/").ToLower();
        }

        bool IsTheSameFilePath(string path1, string path2)
        {
            string p2 = PrepForPathComparison(path2);

            if (path1.CompareTo(p2) == 0)
            {
                return true;
            }

            return false;
        }

        void GUIDrawAndTestButtons()
        {
            int loveSceneIndex = -1;
            int moveUpIndex = -1;
            int moveDownIndex = -1;
            int hideSceneIndex = -1;

            string loadScene = null;
            string runScene = null;

            if (maxHeight < 0.0f)
            {
                maxHeight = this.position.height;
            }

            var logo = SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Logo);
            if (logo == null)
                return;
            float top = logo.height;
            Rect allPosition = new Rect(0.0f, top, position.width, this.position.height - top);
            Rect viewRect = new Rect(0.0f, 0.0f, position.width - 20.0f, maxHeight);

            if (SceneSorterGlobals.ResourcesOk == false)
            {
                GUI.Label(allPosition,
                    "Failed to load some of the resources\nTry reinstalling the Unity Package from the Asset Store");
                return;
            }
            
            // Change between the path styles when clicking the top bar.
            Rect topBarRect = new Rect(0.0f, 0.0f, position.width, top);
            if (topBarRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseUp)
                {
                    Preferences.NextTrimOption();
                    ForceRepaint();
                }
            }

            // Maybe a button asking them to search for scene files? 
            if (allSceneLocations == null)
                return;

            _scrollPos = GUI.BeginScrollView(allPosition, _scrollPos, viewRect);

            bool hideState = Event.current.control;
            if (allSceneLocations != null)
            {
                string loadedScene = PrepForPathComparison(EditorSceneManager.GetActiveScene().path);
                if (string.IsNullOrWhiteSpace(loadedScene) == false)
                {
                    int len = loadedScene.Length;
                    loadedScene = loadedScene.Substring(7, len - 7 - 6);

                    int total = allSceneLocations.Count;
                    int shownTotal = 0;
                    for (int i = 0; i < total; i++)
                    {
                        if( hideState )
                        {
                            // When in hide state we don't show favorites or the current scene?
                            if( allSceneLocations[i].favorite )
                                continue;
                        }
                        else
                        {
                            if (allSceneLocations[i].hidden)
                                continue;
                        }

                        bool currentlyLoadedScene = false;
                        Texture2D loadTexture = SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Load);
                        if (IsTheSameFilePath(loadedScene, allSceneLocations[i].name))
                        {
                            currentlyLoadedScene = true;
                            
                            if( hideState ) // don't show this line in hide state.
                                continue;
                            
                            loadTexture = SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.LoadFilled);
                        }

                        float xPos = SceneSorterGlobals.LeftMargin;
                        float yPos = shownTotal * (SceneSorterGlobals.ButtonHeight + SceneSorterGlobals.VertPad);
                        shownTotal++;
                        
                        #region Clicked on a line item
                        if (!hideState)
                        {
                            // If we click on a line we are on, change its focus to something appropriate.
                            Rect textLineRect = new Rect(_textLineLeft, yPos, _textLineRight - _textLineLeft,
                                SceneSorterGlobals.ButtonHeight);

                            if (textLineRect.Contains(Event.current.mousePosition))
                            {
                                if (Event.current.type == EventType.MouseUp)
                                {
                                    switch (_focusState)
                                    {
                                        case FocusState.NoFocus:
                                            PartialFocus(allSceneLocations[i].name);
                                            break;
                                        case FocusState.PartialFocus:
                                            if (allSceneLocations[i].name != _focusedSceneName)
                                            {
                                                PartialFocus(allSceneLocations[i].name);
                                            }
                                            else
                                            {
                                                NoFocus();
                                            }

                                            break;
                                        case FocusState.FixedFocus:
                                            if (allSceneLocations[i].name != _focusedSceneName)
                                            {
                                                PartialFocus(allSceneLocations[i].name);
                                            }
                                            else
                                            {
                                                NoFocus();
                                            }

                                            break;

                                    }
                                }
                            }
                        }
                        #endregion

                        #region Regular buttons like love, load, play - or those for hide state
                        Rect loveRect = new Rect(xPos, yPos, SceneSorterGlobals.ButtonWidth,
                            SceneSorterGlobals.ButtonHeight);
                        xPos += SceneSorterGlobals.ButtonWidth + SceneSorterGlobals.HorizPad;

                        Rect loadRect = new Rect(xPos, yPos, SceneSorterGlobals.ButtonWidth,
                            SceneSorterGlobals.ButtonHeight);
                        xPos += SceneSorterGlobals.ButtonWidth + SceneSorterGlobals.HorizPad;

                        Rect playRect = new Rect(xPos, yPos, SceneSorterGlobals.ButtonWidth,
                            SceneSorterGlobals.ButtonHeight);
                        xPos += SceneSorterGlobals.ButtonWidth + SceneSorterGlobals.HorizPad;
                        yPos += SceneSorterGlobals.LabelTopMargin;
                        Rect nameRect = new Rect(xPos, yPos - 8.0f, SceneSorterGlobals.NameWidth,
                            SceneSorterGlobals.NameHeight);

                        if (hideState)
                        {
                            if (allSceneLocations[i].hidden)
                            {
                                if (GUI.Button(loadRect,
                                        SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Show)))
                                {
                                    hideSceneIndex = i;
                                }
                            }
                            else
                            {
                                if (GUI.Button(loveRect,
                                        SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Hide)))
                                {
                                    hideSceneIndex = i;
                                }
                            }
                        }
                        else
                        {
                            if (allSceneLocations[i].favorite)
                            {
                                if (GUI.Button(loveRect,
                                        SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.LoveFilled)))
                                {
                                    loveSceneIndex = i;
                                }
                            }
                            else
                            {
                                if (GUI.Button(loveRect,
                                        SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Love)))
                                {
                                    loveSceneIndex = i;
                                }
                            }

                            if (GUI.Button(loadRect, loadTexture))
                            {
                                // Load this scene.
                                loadScene = allSceneLocations[i].name;
                            }

                            if (GUI.Button(playRect, SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Play)))
                            {
                                // Load and play this scene.
                                runScene = allSceneLocations[i].name;
                            }
                        }

                        // Set the GUIStyle style to be label
                        GUIStyle style = GUI.skin.GetStyle("label");
                        if (currentlyLoadedScene)
                            style.fontStyle = FontStyle.Bold;
                        else
                            style.fontStyle = FontStyle.Normal;
                        GUI.Label(nameRect, allSceneLocations[i].trimmedName);

                        #endregion

                        #region For when we have highlighted a line

                        if (!hideState)
                        {
                            // Show move up/down buttons if this is a focused scene.
                            if (allSceneLocations[i].name == _focusedSceneName)
                            {
                                // Some things to draw if we have partial or fixed focus.
                                if (_focusState != FocusState.NoFocus)
                                {
                                    float arrowYPos = yPos + 4.0f;
                                    float arrowXPos = position.width - SceneSorterGlobals.RightMargin -
                                                      SceneSorterGlobals.NoBorderButtonWidth -
                                                      SceneSorterGlobals.NoBorderButtonWidth -
                                                      SceneSorterGlobals.HorizPad -
                                                      10.0f;
                                    Rect downRect = new Rect(arrowXPos, arrowYPos + 4.0f,
                                        SceneSorterGlobals.NoBorderButtonWidth,
                                        SceneSorterGlobals.NoBorderButtonHeight);

                                    arrowXPos += SceneSorterGlobals.NoBorderButtonWidth + SceneSorterGlobals.HorizPad;
                                    Rect upRect = new Rect(arrowXPos, arrowYPos, SceneSorterGlobals.NoBorderButtonWidth,
                                        SceneSorterGlobals.NoBorderButtonHeight);

                                    if (NoBorderButton(downRect,
                                            SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Down)))
                                    {
                                        if (CanMovePosition(i, i + 1))
                                            moveDownIndex = i;
                                    }

                                    if (NoBorderButton(upRect,
                                            SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Up)))
                                    {
                                        if (CanMovePosition(i, i - 1))
                                            moveUpIndex = i;
                                    }

                                    // Draw the focus indicator.
                                    var focusTexture =
                                        SceneSorterGlobals.GetTexture(SceneSorterGlobals.SSGTexture.Focus);
                                    Rect focusRect = new Rect(0, yPos - 8.0f, focusTexture.width, focusTexture.height);
                                    GUI.DrawTexture(focusRect, focusTexture);
                                }
                            }
                        }

                        #endregion
                        
                        // next line.
                        maxHeight = yPos + SceneSorterGlobals.ButtonHeight + SceneSorterGlobals.VertPad;
                    }
                }
            }

            GUI.EndScrollView();

            if (hideSceneIndex >= 0)
            {
                // Hide or unhide this one.
                allSceneLocations[hideSceneIndex].hidden = !allSceneLocations[hideSceneIndex].hidden;
                ForceRepaint();
            }
            else if (loveSceneIndex >= 0)
            {
                // Favorite this index and move it to the top.
                allSceneLocations[loveSceneIndex].favorite = !allSceneLocations[loveSceneIndex].favorite;
                if (allSceneLocations[loveSceneIndex].favorite)
                    FixedFocus(allSceneLocations[loveSceneIndex].name);
                else
                    NoFocus();
                SortSceneLocationsByLove();
                ForceRepaint();
            }
            else if (moveUpIndex > 0)
            {
                // Whatever is at this index position should be moved up one.
                FixedFocus(allSceneLocations[moveUpIndex].name);
                int aboveIdx = moveUpIndex - 1;
                (allSceneLocations![aboveIdx], allSceneLocations[moveUpIndex]) =
                    (allSceneLocations[moveUpIndex], allSceneLocations[aboveIdx]);
                ForceRepaint();
            }
            else if (moveDownIndex >= 0 && moveDownIndex < allSceneLocations.Count - 1)
            {
                // Whatever is at this index position should be moved down one.
                FixedFocus(allSceneLocations[moveDownIndex].name);
                int belowIdx = moveDownIndex + 1;
                (allSceneLocations![belowIdx], allSceneLocations[moveDownIndex]) =
                    (allSceneLocations[moveDownIndex], allSceneLocations[belowIdx]);
                ForceRepaint();
            }
            else if (string.IsNullOrEmpty(loadScene) == false)
            {
                LoadThisScene(loadScene);
                ForceRepaint();
            }
            else if (string.IsNullOrEmpty(runScene) == false)
            {
                RunThisScene(runScene);
                ForceRepaint();
            }
        }
        
        // Start trimming from the center and work our way out.
        private string TrimStringByCenter(string text, int maxWidth)
        {
            GUIStyle labelStyle = EditorStyles.label;
            float textWidth = labelStyle.CalcSize(new GUIContent(text)).x;
            
            if( textWidth < maxWidth )
                return text;
            
            string trimmedText = text;

            int leftIndex = (text.Length / 2)-1;
            int rightIndex = (text.Length / 2)+1;
            
            while( leftIndex > 0 && rightIndex < text.Length )
            {
                trimmedText = text.Substring(0, leftIndex) + "..." + text.Substring(rightIndex);
                textWidth = labelStyle.CalcSize(new GUIContent(trimmedText)).x;
                if( textWidth < maxWidth )
                    break;
                leftIndex--;
                rightIndex++;
            }

            if (textWidth > maxWidth)
                trimmedText = "";

            return trimmedText;
        }
        
        
        // Just keep the scene name itself.
        private string TrimStringToNameOnly(string text)
        {
            var tokens = text.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (tokens == null || tokens.Length == 0)
                return text;
            
            return tokens[^1];
        }

        // Keep as much of the scene name as we can, drop any characters off the end.
        private string TrimStringShowStart(string text, int maxWidth)
        {
            GUIStyle labelStyle = EditorStyles.label;
            float textWidth = labelStyle.CalcSize(new GUIContent(text)).x;
            
            if( textWidth < maxWidth )
                return text;
            
            string trimmedText = text;

            int trimLength = text.Length - 1;

            while( trimLength > 0)
            {
                trimmedText = text.Substring(0, trimLength);
                textWidth = labelStyle.CalcSize(new GUIContent(trimmedText)).x;
                if( textWidth < maxWidth )
                    break;
                trimLength--;
            }

            if (textWidth > maxWidth)
                trimmedText = "";

            return trimmedText;
        }
            
        
        // Start trimming the front until we fit.
        private string TrimStringShowEnd(string text, int maxWidth)
        {
            GUIStyle labelStyle = EditorStyles.label;
            float textWidth = labelStyle.CalcSize(new GUIContent(text)).x;
            
            if( textWidth < maxWidth )
                return text;
            
            string trimmedText = text;

            int trimIndex = 3;

            while( trimIndex < text.Length )
            {
                trimmedText = "..." + text.Substring(trimIndex);
                textWidth = labelStyle.CalcSize(new GUIContent(trimmedText)).x;
                if( textWidth < maxWidth )
                    break;
                trimIndex++;
            }

            if (textWidth > maxWidth)
                trimmedText = "";

            return trimmedText;
        }

        private static bool CanMovePosition(int curIdx, int targetIdx)
        {
            if (targetIdx < 0 || targetIdx >= allSceneLocations.Count)
                return false;

            // Non favorites cant move past favorites and vice versa.
            if (allSceneLocations[curIdx].favorite != allSceneLocations[targetIdx].favorite)
                return false;
            return true;
        }

        private static void ForceRepaint()
        {
            _forceRepaint = true;
        }

        private bool NoBorderButton(Rect rect, Texture2D texture)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = texture;
            buttonStyle.border = new RectOffset(0, 0, 0, 0);
            return GUI.Button(rect, texture, buttonStyle);
        }

        private void SortSceneLocationsByLove()
        {
            // Sort "favorite" tags to the top.
            int total = allSceneLocations.Count;
            if (total == 0)
                return;

            List<SceneSorterElement> newList = new List<SceneSorterElement>(total);
            // Add the faves first.
            for (int i = 0; i < total; i++)
            {
                if (allSceneLocations[i].favorite == true)
                    newList.Add(allSceneLocations[i]);
            }

            // Add everything else.
            for (int i = 0; i < total; i++)
            {
                if (allSceneLocations[i].favorite == false)
                    newList.Add(allSceneLocations[i]);
            }

            allSceneLocations = newList;
        }

        #endregion

        #region Scene Actions

        void LoadThisScene(string sceneName)
        {
            if (EditorApplication.isPlaying == false && EditorApplication.isPaused == false)
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene("Assets/" + sceneName + ".unity");
            }
            else
            {
                EditorApplication.isPaused = false;
                EditorApplication.isPlaying = false;
            }
        }

        void RunThisScene(string sceneName)
        {
            if (EditorApplication.isPlaying == false && EditorApplication.isPaused == false)
            {
                EditorPrefs.SetString(_lastSceneKey, EditorSceneManager.GetActiveScene().path);

                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene("Assets/" + sceneName + ".unity");

                // Start play mode.
                EditorApplication.isPlaying = true;
            }
            else
            {
                EditorApplication.isPaused = false;
                EditorApplication.isPlaying = false;
            }
        }

        #endregion


        [MenuItem("Tools/Scene Sorter/Run Scene 1 &1")]
        public static void RunScene1()
        {
            QuickRunSceneAtIndex(0);
        }

        [MenuItem("Tools/Scene Sorter/Run Scene 2 &2")]
        public static void RunScene2()
        {
            QuickRunSceneAtIndex(1);
        }

        [MenuItem("Tools/Scene Sorter/Run Scene 3 &3")]
        public static void RunScene3()
        {
            QuickRunSceneAtIndex(2);
        }

        private static string _lastSceneKey = null;

        private static string GetLastSceneKey()
        {
            if (_lastSceneKey == null)
                _lastSceneKey = SceneSorterSerializer.GetKey() + "lastScene";
            return _lastSceneKey;
        }

        private static void QuickRunSceneAtIndex(int idx)
        {
            if (allSceneLocations != null && idx < allSceneLocations.Count)
            {
                if (EditorApplication.isPlaying == false && EditorApplication.isPaused == false)
                {

                    EditorPrefs.SetString(_lastSceneKey, EditorSceneManager.GetActiveScene().path);

                    EditorSceneManager.SaveOpenScenes();
                    EditorSceneManager.OpenScene("Assets/" + allSceneLocations[idx].name + ".unity");
                    EditorApplication.isPlaying = true;
                }
            }
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                string lastScene = EditorPrefs.GetString(_lastSceneKey, "");
                if (string.IsNullOrWhiteSpace(lastScene) == false)
                {
                    Debug.Log("Restoring scene " + lastScene.ToString());
                    EditorSceneManager.OpenScene(lastScene);
                    EditorPrefs.SetString(_lastSceneKey, "");
                }
            }
        }

        public static void ShowHiddenScenes()
        {
            foreach (var element in allSceneLocations)
            {
                if (element.hidden)
                    element.hidden = false;
            }

            ForceRepaint();
        }
    }
}