using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedCandleGames.Editor
{
    /// <summary>
    /// Alternative minimal injection mode that only adds a small search button
    /// This is more compatible but less integrated than the full search bar
    /// </summary>
    [InitializeOnLoad]
    public static class AnimationWindowMinimalMode
    {
        private static bool useMinimalMode = true; // Set to true for minimal mode
        private static Button searchButton;
        private static EditorWindow lastAnimationWindow;
        
        static AnimationWindowMinimalMode()
        {
            if (useMinimalMode)
            {
                EditorApplication.update += OnEditorUpdate;
            }
        }
        
        private static void OnEditorUpdate()
        {
            if (!useMinimalMode) return;
            
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
                
                // Position in top-right corner
                searchButton.style.position = Position.Absolute;
                searchButton.style.top = 2;
                searchButton.style.right = 25; // Leave space for window controls
                searchButton.style.width = 25;
                searchButton.style.height = 20;
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
        
        [MenuItem("Window/Animation/Toggle Minimal Mode")]
        private static void ToggleMinimalMode()
        {
            useMinimalMode = !useMinimalMode;
            EditorPrefs.SetBool("AnimationClipSearch_MinimalMode", useMinimalMode);
            
            if (useMinimalMode)
            {
                Debug.Log("Animation Clip Search: Switched to minimal mode (button only)");
                // Disable full injection
                AnimationWindowUIInjector.DisableInjection();
            }
            else
            {
                Debug.Log("Animation Clip Search: Switched to full mode (search bar)");
                CleanupMinimalUI();
            }
        }
    }
}