using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Porkchop.SceneSorter
{
    public static class Preferences
    {
        public enum NameTrimOption
        {
            FullPath,
            ShowEnd,
            TrimCenter,
            NameOnly,
        }
        
        private static readonly string KeyWindowIcon     = "PC/SS/WI";
        private static readonly string KeyTrimOption     = "PC/SS/TO";
        private static bool _prefsLoaded = false;
        private static bool _windowIcon = false;
        private static NameTrimOption _trimOption = NameTrimOption.ShowEnd;

        public static bool WindowIcon  { get { LoadPrefs(); return _windowIcon; } }
        public static NameTrimOption TrimOption  { get { LoadPrefs(); return _trimOption; } }
        public static bool TriggerTrim { get; set; } = false;

        private static void LoadPrefs()
        {
            if( !_prefsLoaded )
            {
                _windowIcon = EditorPrefs.GetBool( KeyWindowIcon, false );
                _trimOption = (NameTrimOption) EditorPrefs.GetInt(KeyTrimOption, (int)NameTrimOption.ShowEnd);
                TriggerTrim = true;
                _prefsLoaded = true;
            }
        }

        public class SceneSorterSettings : SettingsProvider
        {
            public SceneSorterSettings( string path, SettingsScope scopes = SettingsScope.User, IEnumerable<string> keywords = null )
                : base( path, scopes, keywords )
            {
            }

            public override void OnGUI( string searchContext )
            {
                DrawPreferences();
            }
        }

        [SettingsProvider]
        static SettingsProvider SceneSorterPreferences()
        {
            return new SceneSorterSettings( "Preferences/Scene Sorter" );
        }

        private static void DrawPreferences()
        {
            LoadPrefs();

            EditorGUI.BeginChangeCheck();

            NameTrimOption lastTrimSetting = _trimOption;
            _windowIcon  = EditorGUILayout.Toggle( "Window icon", _windowIcon );

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Show scene names", GUILayout.Width(150));
            _trimOption = (NameTrimOption)EditorGUILayout.EnumPopup(_trimOption);
            GUILayout.EndHorizontal();
            
            if (_trimOption != lastTrimSetting)
            {
                TriggerTrim = true;
            }
            
            EditorGUI.EndChangeCheck();
            
            GUILayout.Space( 10 );
            GUILayout.Label(
                "In the Scene Sorter window, hold Ctrl to toggle a scenes visibility. Click the button below to reset so all scenes are visible.", EditorStyles.wordWrappedLabel);
            
            if( GUILayout.Button("Show hidden scenes") )
            {
                SceneSorterEditor.ShowHiddenScenes();
            }

            if( GUI.changed )
            {
                EditorPrefs.SetBool( KeyWindowIcon, _windowIcon );
                EditorPrefs.SetInt( KeyTrimOption, (int)_trimOption);
            }
        }

        public static void NextTrimOption()
        {
            _trimOption = (NameTrimOption) (((int)_trimOption + 1) % 4);
            EditorPrefs.SetInt( KeyTrimOption, (int)_trimOption);
            TriggerTrim = true;
        }
    }
}
