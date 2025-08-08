using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedCandleGames.Editor
{
    /// <summary>
    /// Adds a small search button to the Animation Window
    /// </summary>
    [InitializeOnLoad]
    public static class AnimationWindowMinimalMode
    {
        private static Button searchButton;
        private static EditorWindow lastAnimationWindow;
        
        static AnimationWindowMinimalMode()
        {
            EditorApplication.update += OnEditorUpdate;
        }
        
        private static void OnEditorUpdate()
        {
            var animationWindow = GetAnimationWindow();
            
            if (animationWindow != null)
            {
                if (animationWindow != lastAnimationWindow || searchButton == null)
                {
                    InjectMinimalUI(animationWindow);
                    lastAnimationWindow = animationWindow;
                }
            }
            else
            {
                CleanupMinimalUI();
            }
        }
        
        private static EditorWindow GetAnimationWindow()
        {
            var assembly = typeof(EditorWindow).Assembly;
            var animationWindowType = assembly.GetType("UnityEditor.AnimationWindow");
            
            if (animationWindowType != null)
            {
                var windows = Resources.FindObjectsOfTypeAll(animationWindowType);
                if (windows.Length > 0)
                {
                    return windows[0] as EditorWindow;
                }
            }
            
            return null;
        }
        
        private static void InjectMinimalUI(EditorWindow animWindow)
        {
            try
            {
                var rootVisualElement = animWindow.rootVisualElement;
                
                if (rootVisualElement == null)
                {
                    return;
                }
                
                // Check if already injected
                if (searchButton != null && rootVisualElement.Contains(searchButton))
                {
                    return;
                }
                
                // Create minimal search button
                searchButton = new Button(OpenSearchWindow);
                searchButton.name = "AnimationClipSearchButton";
                searchButton.text = "🔍";
                searchButton.tooltip = "Search Animation Clips (Alt+S)";
                
                // Position in bottom-left corner
                searchButton.style.position = Position.Absolute;
                searchButton.style.bottom = 5;
                searchButton.style.left = 5;
                searchButton.style.width = 30;
                searchButton.style.height = 22;
                searchButton.style.fontSize = 14;
                searchButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
                searchButton.style.borderTopLeftRadius = 3;
                searchButton.style.borderTopRightRadius = 3;
                searchButton.style.borderBottomLeftRadius = 3;
                searchButton.style.borderBottomRightRadius = 3;
                
                // Hover effect
                searchButton.RegisterCallback<MouseEnterEvent>(e => 
                    searchButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.9f));
                searchButton.RegisterCallback<MouseLeaveEvent>(e => 
                    searchButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f));
                
                rootVisualElement.Add(searchButton);
                
                Debug.Log("Minimal search button injected into Animation Window");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to inject minimal UI: {e.Message}");
            }
        }
        
        private static void OpenSearchWindow()
        {
            AnimationClipSearchTool.ShowWindow();
        }
        
        private static void CleanupMinimalUI()
        {
            if (searchButton != null && searchButton.parent != null)
            {
                searchButton.parent.Remove(searchButton);
            }
            
            searchButton = null;
            lastAnimationWindow = null;
        }
    }
}