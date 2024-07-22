using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Resilience.LetMeSee
{
    public static class LetMeSeeLocalization
    {
        private const string EditorLanguageNonLocalized = "Editor Language";
        private const string MetaLanguageNameKey = "_meta_languageName";
        private const string MetaGptKey = "_meta_x_gpt";
        
        private const string Prefix = "let-me-see.";
        private const string Suffix = ".json";
        private const string LetMeSeeLocalePrefsKey = LetMeSeeCore.Prefix + ".Locale";
        private const string LocaleLocation = "Packages/dev.hai-vr.resilience.let-me-see/Locale";
        private const string LocaleLocation2 = "Assets/ResilienceSDK/LetMeSee/Locale";

        private static string _selectedLanguageCode = "en";
        private static int _selectedIndex;
        
        private static List<string> _availableLanguageNames = new List<string> { "English" };
        private static Dictionary<string, Dictionary<string, string>> _languageCodeToLocalization;
        private static List<string> _availableLanguageCodes;
        
        private static readonly Dictionary<string, string> DebugKeyDatabase = new Dictionary<string, string>();
        
        static LetMeSeeLocalization()
        {
            DebugKeyDatabase.Add(MetaLanguageNameKey, "English");
            DebugKeyDatabase.Add(MetaGptKey, @"These can be translated with ChatGPT using the prompt: Please translate the values of this JSON file to language written in the _meta_languageName key. Keep the keys intact. The value of the first key `_meta_languageName` also needs to be translated to that language (for example, French needs to be Français), and then concatenated with the string ` (ChatGPT)` ");
            // DebugKeyDatabase.Add(MetaGptKey, @"These can be translated with ChatGPT using the prompt: Please translate the values of this JSON file to XXXXXXXXX language. Keep the keys intact. The value of the first key `_meta_languageName` needs to be changed to match the XXXXXXXXX language, concatenated with the string ` (ChatGPT)`");
            
            ReloadLocalizationsInternal();
            var confLocale = EditorPrefs.GetString(LetMeSeeLocalePrefsKey);
            var languageCode = string.IsNullOrEmpty(confLocale) ? "en" : confLocale;
            if (_languageCodeToLocalization.ContainsKey(languageCode))
            {
                _selectedLanguageCode = languageCode;
            }

            _selectedIndex = _selectedLanguageCode == "en" ? 0 : 1;

            INTROSPECT_INVOKE_ALL(typeof(LetMeSeeLocalizationPhrase));
            // PrintDatabase();
        }

        public static void DisplayLanguageSelector()
        {
            var selectedLanguage = EditorGUILayout.Popup(new GUIContent(EditorLanguageNonLocalized), ActiveLanguageIndex(), AvailableLanguages());
            if (selectedLanguage != ActiveLanguageIndex())
            {
                SwitchLanguage(selectedLanguage);
            }
        }

        public static int ActiveLanguageIndex()
        {
            return _selectedIndex;
        }

        public static string[] AvailableLanguages()
        {
            return _availableLanguageNames.ToArray();
        }

        public static void SwitchLanguage(int selectedLanguage)
        {
            var languageCode = _availableLanguageCodes[selectedLanguage];
            _selectedLanguageCode = languageCode;
            _selectedIndex = selectedLanguage;
            EditorPrefs.SetString(LetMeSeeLocalePrefsKey, languageCode);
        }

        private static void ReloadLocalizationsInternal()
        {
            var localizationGuids = AssetDatabase.FindAssets("", new[] { LocaleLocation, LocaleLocation2 });
            _languageCodeToLocalization = localizationGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path =>
                {
                    var fileName = Path.GetFileName(path);
                    return fileName.StartsWith(Prefix) && fileName.EndsWith(Suffix);
                })
                .Where(path =>
                {
                    var fileName = Path.GetFileName(path);
                    var languageCode = fileName.Substring(Prefix.Length, fileName.Length - Prefix.Length - Suffix.Length);
                    return languageCode != "en";
                })
                .ToDictionary(path =>
                {
                    var fileName = Path.GetFileName(path);
                    var languageCode = fileName.Substring(Prefix.Length, fileName.Length - Prefix.Length - Suffix.Length);
                    return languageCode;
                }, ExtractDictionaryFromPath);

            _availableLanguageCodes = new[] { "en" }
                .Concat(_languageCodeToLocalization.Keys)
                .ToList();
            _availableLanguageNames = new[] { "English" }
                .Concat(_languageCodeToLocalization.Values.Select(dictionary => (dictionary.TryGetValue(MetaLanguageNameKey, out var value) ? value : "??")))
                .ToList();
        }

        private static Dictionary<string, string> ExtractDictionaryFromPath(string path)
        {
            try
            {
                var contents = File.ReadAllText(path);
                return ExtractDictionaryFromText(contents);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return new Dictionary<string, string>();
            }
        }

        private static Dictionary<string, string> ExtractDictionaryFromText(string contents)
        {
            var localizations = new Dictionary<string, string>();
            
            // Assume that NewtonsoftJson is available in the project
            var jsonObject = JObject.Parse(contents);
            foreach (var pair in jsonObject)
            {
                var value = pair.Value.Value<string>();
                localizations.Add(pair.Key, value);
            }

            return localizations;
        }
        
        // UI

        private static bool IsEnglish()
        {
            return _selectedIndex == 0;
        }

        internal static string LocalizeOrElse(string labelName, string orDefault)
        {
            var key = $"label_{labelName}";
            return DoLocalize(orDefault, key);
        }

        private static string DoLocalize(string orDefault, string key)
        {
            RegisterInKeyDb(orDefault, key);
            if (IsEnglish()) return orDefault;
            if (_languageCodeToLocalization[_selectedLanguageCode].TryGetValue(key, out var value)) return value;
            return orDefault;
        }

        private static void RegisterInKeyDb(string orDefault, string key)
        {
            if (!DebugKeyDatabase.ContainsKey(key))
            {
                DebugKeyDatabase.Add(key, orDefault);

                // PrintDatabase();
            }
        }

        private static void PrintDatabase()
        {
            var sorted = new SortedDictionary<string, string>(DebugKeyDatabase);
            var jsonObject = JObject.FromObject(sorted);
            Debug.Log(jsonObject.ToString());
        }

        private static void INTROSPECT_INVOKE_ALL(Type type)
        {
            foreach (var methodInfo in type.GetMethods()
                         .Where(info => info.ReturnType == typeof(string))
                         .Where(info => info.IsStatic)
                     )
            {
                methodInfo.Invoke(null, Array.Empty<object>());
            }
        }
    }
}