using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TenorGifToolkit.Core;
using TenorGifToolkit.Helpers;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TenorGifSearcher))]
public class TenorGifSearcherEditor : Editor
{
    private TenorGifSearcher Target { get; set; }

    private int searchEventMethodIndex;
    private const BindingFlags searchEventMethodFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
    private SerializedProperty onSearchedTargetMethodSelector;
    private SerializedProperty onSearchedTargetComponent;
    private SerializedProperty tenorApiKey;

    private void OnEnable()
    {
        Target = target as TenorGifSearcher;
        onSearchedTargetMethodSelector = serializedObject.FindProperty("onSearchedTargetMethodSelector");
        onSearchedTargetComponent = serializedObject.FindProperty("onSearchedTargetComponent");
        tenorApiKey = serializedObject.FindProperty("tenorApiKey");
    }

    public class Testy
    {
        public Component target;
        public MethodInfo methodInfo;
    }

    public override void OnInspectorGUI()
    {
        Color defaultBackgroundColour = GUI.backgroundColor;
        Color headerBackgroundColour = new Color(0,0,0,0.75f);

        //DrawDefaultInspector();
        //base.OnInspectorGUI();

        serializedObject.Update();
        EditorGUILayout.Space();

        GUI.backgroundColor = headerBackgroundColour;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Tenor API");
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = defaultBackgroundColour;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(tenorApiKey);
        if (tenorApiKey.stringValue == "LIVDSRZULELA")
        {
            EditorGUILayout.Space();
            GUILayout.Label("\"LIVDSRZULELA\" is a public key for testing only, Tenor may deactivate this key at any time.", EditorStyles.boldLabel);
            GUILayout.Label("You will need to get your own, by registering at the site listed below", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }
        if (GUILayout.Button("https://tenor.com/developer/dashboard", EditorStyles.linkLabel))
        {
            Application.OpenURL("https://tenor.com/developer/dashboard");
        }
        if (GUILayout.Button("Test API Key"))
        {
            TenoreServiceAPI.TestApiKey(tenorApiKey.stringValue);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        GUI.backgroundColor = headerBackgroundColour;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Search Parameters");
        GUILayout.Label("Used by the Handler when making searches on the API", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = defaultBackgroundColour;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Target.searchLimit = EditorGUILayout.IntSlider("Search Limit", Target.searchLimit, 1, 50);
        Target.contentFilter = (SearchContentFilters)EditorGUILayout.EnumPopup("Content Filter", Target.contentFilter);
        string description;
        switch (Target.contentFilter)
        {
            case SearchContentFilters.High:
                description = "Only show content rated: G";
                break;
            case SearchContentFilters.Medium:
                description = "Only show content rated: G and PG";
                break;
            case SearchContentFilters.Low:
                description = "Only show content rated: G, PG and PG-13";
                break;
            case SearchContentFilters.Off:
                description = "Only show content rated: G, PG, PG-13, and R (no nudity)";
                break;
            default:
                description = "Not Set";
                break;
        }
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
        GUILayout.Label(description, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndHorizontal();
        Target.randomSearchKeyword = EditorGUILayout.TextField("Random Search Keyword", Target.randomSearchKeyword);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
        GUILayout.Label("Used when requesting random GIFs", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        GUI.backgroundColor = headerBackgroundColour;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("On Searched Event");
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = defaultBackgroundColour;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Only public methods with of (TenorAPI.SearchResults results, Boolean nextPages) can be selected.", EditorStyles.miniLabel);
        EditorGUILayout.BeginVertical();
        Target.OnSearchedTargetObject = (GameObject)EditorGUILayout.ObjectField(Target.OnSearchedTargetObject, typeof(GameObject), true);
        if (Target.OnSearchedTargetObject)
        {
            Dictionary<string, Testy> methodList = new Dictionary<string, Testy>();
            methodList.Add("Not Selected", new Testy() {  });

            var componenents = Target.OnSearchedTargetObject.GetComponents<Component>().ToList();
            if (componenents.Count > 0)
            {
                GenericMenu menu = new GenericMenu();
                componenents.ForEach(component =>
                {
                    component.GetType().GetMethods(searchEventMethodFlags).ToList().ForEach(method => {
                        ParameterInfo[] pramInfos = method.GetParameters();
                        if (pramInfos.Length == 2 && pramInfos[0].ParameterType == typeof(TenorFormattedResults) && pramInfos[1].ParameterType == typeof(bool))
                        {
                            string label = $"{component.GetType()}/{ method.Name}(" +
                                        $"{string.Join(", ", pramInfos.Select(x => $"{x.ParameterType.Name} {x.Name}"))}" +
                                        $")";
                            methodList.Add(label, new Testy() {
                                target = component,
                                methodInfo = method,
                            });;
                        }
                    });
                });
            }


            searchEventMethodIndex = (methodList.TryGetValue(onSearchedTargetMethodSelector.stringValue, out Testy selectedMethodinfo)) ?
                searchEventMethodIndex = methodList.Values.ToList().IndexOf(selectedMethodinfo) : 0;

            searchEventMethodIndex = EditorGUILayout.Popup(searchEventMethodIndex, methodList.Select(x => x.Key).ToArray());

            onSearchedTargetMethodSelector.stringValue = methodList.ElementAt(searchEventMethodIndex).Key;

            Target.OnSearchedTargetMethod = (searchEventMethodIndex == 0) ? null : methodList[onSearchedTargetMethodSelector.stringValue].methodInfo;
            onSearchedTargetComponent.objectReferenceValue = (searchEventMethodIndex == 0) ? null : methodList[onSearchedTargetMethodSelector.stringValue].target;
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        GUI.backgroundColor = headerBackgroundColour;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("NOTE: The use of Tenor and its API requires proper attribution.");
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = defaultBackgroundColour;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (GUILayout.Button("https://tenor.com/gifapi/documentation#attribution", EditorStyles.linkLabel))
        {
            Application.OpenURL("https://tenor.com/gifapi/documentation#attribution");
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
