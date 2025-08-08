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
                searchButton.text = "S";
                searchButton.tooltip = "Search Animation Clips (Alt+S)";
                
                // Position in bottom-left corner with slight bottom padding
                searchButton.style.position = Position.Absolute;
                searchButton.style.bottom = 2;
                searchButton.style.left = 0;
                searchButton.style.width = 22;
                searchButton.style.height = 22;
                searchButton.style.fontSize = 11;
                searchButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                searchButton.style.paddingLeft = 0;
                searchButton.style.paddingRight = 0;
                searchButton.style.paddingTop = 0;
                searchButton.style.paddingBottom = 0;
                searchButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
                searchButton.style.borderTopLeftRadius = 2;
                searchButton.style.borderTopRightRadius = 2;
                searchButton.style.borderBottomLeftRadius = 2;
                searchButton.style.borderBottomRightRadius = 2;
                
                // Hover effect - more subtle for smaller button
                searchButton.RegisterCallback<MouseEnterEvent>(e => 
                    searchButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.8f));
                searchButton.RegisterCallback<MouseLeaveEvent>(e => 
                    searchButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.6f));
                
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