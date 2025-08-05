using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace RedCandleGames.Editor
{
    [InitializeOnLoad]
    public static class AnimationWindowExtensions
    {
        private static MethodInfo openAnimationWindowMethod;
        
        static AnimationWindowExtensions()
        {
            // Hook into the editor update
            EditorApplication.update += OnEditorUpdate;
        }
        
        private static void OnEditorUpdate()
        {
            // Check if Animation window is open and if Alt+S is pressed
            if (Event.current != null && Event.current.type == EventType.KeyDown)
            {
                // Alt+S for search (避免與 Ctrl/Cmd+F 的搜尋衝突)
                if (Event.current.alt && Event.current.keyCode == KeyCode.S)
                {
                    var animationWindow = GetAnimationWindow();
                    if (animationWindow != null && EditorWindow.focusedWindow == animationWindow)
                    {
                        // Open our search tool
                        AnimationClipSearchTool.ShowWindowWithCallback(OnClipSelectedFromSearch);
                        Event.current.Use();
                    }
                }
            }
        }
        
        private static EditorWindow GetAnimationWindow()
        {
            System.Type animationWindowType = System.Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
            if (animationWindowType != null)
            {
                UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(animationWindowType);
                if (windows.Length > 0)
                {
                    return windows[0] as EditorWindow;
                }
            }
            return null;
        }
        
        private static void OnClipSelectedFromSearch(AnimationClip clip)
        {
            if (clip == null) return;
            
            var animationWindow = GetAnimationWindow();
            if (animationWindow != null)
            {
                // First, ensure the Animation window is focused
                animationWindow.Focus();
                
                // Try multiple approaches to set the clip
                if (!TrySetClipViaProperty(animationWindow, clip))
                {
                    if (!TrySetClipViaSelection(clip))
                    {
                        // Last resort: ping the object
                        Selection.activeObject = clip;
                        EditorGUIUtility.PingObject(clip);
                        Debug.Log($"Selected animation clip: {clip.name}. You may need to drag it to the Animation window.");
                    }
                }
            }
        }
        
        private static bool TrySetClipViaProperty(EditorWindow animWindow, AnimationClip clip)
        {
            var type = animWindow.GetType();
            
            // Try different property names that might exist
            string[] propertyNames = { "animationClip", "activeAnimationClip", "selection" };
            
            foreach (string propName in propertyNames)
            {
                var property = type.GetProperty(propName, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        property.SetValue(animWindow, clip, null);
                        animWindow.Repaint();
                        Debug.Log($"Successfully applied animation clip: {clip.name}");
                        return true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to set property {propName}: {e.Message}");
                    }
                }
            }
            
            return false;
        }
        
        private static bool TrySetClipViaSelection(AnimationClip clip)
        {
            GameObject activeGO = Selection.activeGameObject;
            if (activeGO != null)
            {
                // For Animator
                Animator animator = activeGO.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    Selection.activeObject = clip;
                    EditorGUIUtility.PingObject(clip);
                    
                    // Force refresh
                    EditorWindow animWindow = GetAnimationWindow();
                    if (animWindow != null)
                    {
                        animWindow.Repaint();
                    }
                    
                    Debug.Log($"Selected clip: {clip.name} for Animator on {activeGO.name}");
                    return true;
                }
                
                // For Legacy Animation
                Animation legacyAnim = activeGO.GetComponent<Animation>();
                if (legacyAnim != null)
                {
                    legacyAnim.clip = clip;
                    EditorUtility.SetDirty(activeGO);
                    Debug.Log($"Applied animation clip to Legacy Animation: {clip.name}");
                    return true;
                }
            }
            
            return false;
        }
    }
    
    // Alternative approach using menu items
    public static class AnimationWindowMenuItems
    {
        [MenuItem("Window/Animation/Search Animation Clips &s")]
        private static void OpenAnimationClipSearch()
        {
            AnimationClipSearchTool.ShowWindow();
        }
        
        [MenuItem("Animation/Search Clips", false, 1000)]
        private static void SearchClipsFromAnimationMenu()
        {
            AnimationClipSearchTool.ShowWindow();
        }
    }
}