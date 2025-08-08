using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedCandleGames.Editor
{
    public static class AnimationWindowUIInjectorDebug
    {
        [MenuItem("Window/Animation/Debug UI Injection")]
        private static void DebugUIInjection()
        {
            Debug.Log("=== Animation Window UI Injection Debug Info ===");
            Debug.Log($"Unity Version: {Application.unityVersion}");
            
            // Get Animation Window
            var assembly = typeof(EditorWindow).Assembly;
            var animationWindowType = assembly.GetType("UnityEditor.AnimationWindow");
            
            if (animationWindowType == null)
            {
                Debug.LogError("Could not find AnimationWindow type");
                return;
            }
            
            Debug.Log($"AnimationWindow type found: {animationWindowType.FullName}");
            
            var windows = Resources.FindObjectsOfTypeAll(animationWindowType);
            Debug.Log($"Found {windows.Length} Animation Window(s)");
            
            if (windows.Length == 0)
            {
                Debug.LogWarning("No Animation Window is open. Please open one and try again.");
                return;
            }
            
            foreach (var window in windows)
            {
                var animWindow = window as EditorWindow;
                if (animWindow == null) continue;
                
                Debug.Log($"\nInspecting Animation Window: {animWindow.GetType().Name}");
                
                // Check rootVisualElement
                try
                {
                    var rootVisualElement = animWindow.rootVisualElement;
                    if (rootVisualElement != null)
                    {
                        Debug.Log($"✓ rootVisualElement exists (Type: {rootVisualElement.GetType().Name})");
                        Debug.Log($"  - Children count: {rootVisualElement.childCount}");
                        Debug.Log($"  - Style display: {rootVisualElement.style.display}");
                        Debug.Log($"  - Resolved style display: {rootVisualElement.resolvedStyle.display}");
                        
                        // Try to list immediate children
                        Debug.Log("  - Immediate children:");
                        for (int i = 0; i < Math.Min(5, rootVisualElement.childCount); i++)
                        {
                            var child = rootVisualElement[i];
                            Debug.Log($"    [{i}] {child.GetType().Name} (name: '{child.name}', class: '{string.Join(" ", child.GetClasses())}')");
                            
                            // Check if it's an IMGUI container
                            if (child.GetType().Name == "IMGUIContainer" || child.ClassListContains("unity-imgui-container"))
                            {
                                Debug.Log($"      -> This is an IMGUI container - Animation Window content might be here");
                            }
                        }
                        
                        // Deep search for specific elements
                        Debug.Log("\n  - Deep search for content areas:");
                        var imguiContainers = rootVisualElement.Query(className: "unity-imgui-container").ToList();
                        Debug.Log($"    Found {imguiContainers.Count} IMGUI containers");
                        
                        var contentAreas = rootVisualElement.Query<VisualElement>().Where(e => 
                            e.name != null && (e.name.Contains("content") || e.name.Contains("Content"))).ToList();
                        Debug.Log($"    Found {contentAreas.Count} elements with 'content' in name");
                        
                        foreach (var content in contentAreas)
                        {
                            Debug.Log($"      - {content.name} (parent: {content.parent?.name ?? "null"})");
                        }
                        
                        // Check if our injected element exists
                        var searchContainer = rootVisualElement.Q("AnimationClipSearchContainer");
                        if (searchContainer != null)
                        {
                            Debug.Log("✓ Search container already injected!");
                        }
                        else
                        {
                            Debug.Log("✗ Search container not found - injection may have failed or not been attempted");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("✗ rootVisualElement is null");
                        
                        // Check if this is an IMGUI-only window
                        var hasUIElementsSupport = animWindow.GetType().GetProperty("hasUIElements", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (hasUIElementsSupport != null)
                        {
                            var value = hasUIElementsSupport.GetValue(animWindow);
                            Debug.Log($"  - hasUIElements: {value}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error accessing rootVisualElement: {e.Message}");
                }
                
                // Check AnimationWindowState
                try
                {
                    var stateProperty = animationWindowType.GetProperty("state", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (stateProperty != null)
                    {
                        var state = stateProperty.GetValue(animWindow);
                        Debug.Log($"✓ AnimationWindowState exists: {state != null}");
                    }
                    else
                    {
                        Debug.LogWarning("✗ Could not find 'state' property");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error accessing AnimationWindowState: {e.Message}");
                }
            }
            
            Debug.Log("\n=== Possible Issues ===");
            Debug.Log("1. Unity 6 might have changed the internal structure of Animation Window");
            Debug.Log("2. The window might be using pure IMGUI without UI Toolkit support");
            Debug.Log("3. The rootVisualElement might be initialized later in the window lifecycle");
            Debug.Log("4. Different Unity versions may have different Animation Window implementations");
            
            Debug.Log("\n=== Recommendations ===");
            Debug.Log("- Use the popup search window (Alt+S) as a reliable alternative");
            Debug.Log("- The integrated UI injection is experimental and may not work in all Unity versions");
        }
        
        [MenuItem("Window/Animation/Force UI Injection Retry")]
        private static void ForceUIInjectionRetry()
        {
            // Clear injection state to force retry
            var injectorType = typeof(AnimationWindowUIInjector);
            var isInjectedField = injectorType.GetField("isInjected", BindingFlags.NonPublic | BindingFlags.Static);
            var lastWindowField = injectorType.GetField("lastAnimationWindow", BindingFlags.NonPublic | BindingFlags.Static);
            
            if (isInjectedField != null)
            {
                isInjectedField.SetValue(null, false);
                Debug.Log("Reset injection state");
            }
            
            if (lastWindowField != null)
            {
                lastWindowField.SetValue(null, null);
                Debug.Log("Reset last window reference");
            }
            
            Debug.Log("Injection retry has been scheduled. The system will attempt to inject again when it detects the Animation Window.");
        }
    }
}